using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MuseumTickets.Tests.E2E
{
    [TestFixture]
    [Category("AppendOnlyE2E")]
    [NonParallelizable]
    public class AppendOnlyMuseumE2ETests : PageTest
    {
        private string BaseUrl => (Environment.GetEnvironmentVariable("E2E_BASEURL") ?? "http://localhost:7036").TrimEnd('/');

        private static string Unique(string prefix)
            => $"{prefix} {DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6]}";
        private async Task ClickFirstButtonAsync(params string[] names)
        {
            foreach (var n in names)
            {
                var btn = Page.GetByRole(AriaRole.Button, new() { Name = n });
                if (await btn.CountAsync() > 0 && await btn.First.IsVisibleAsync())
                {
                    await btn.First.ClickAsync();
                    return;
                }
            }
            foreach (var n in names)
            {
                var t = Page.GetByText(n, new() { Exact = false });
                if (await t.CountAsync() > 0 && await t.First.IsVisibleAsync())
                {
                    await t.First.ClickAsync();
                    return;
                }
            }

            Assert.Fail($"Nisam našao nijedno dugme: [{string.Join(", ", names)}].");
        }

        private async Task FillByLabelOrIdAsync(string[] labelCandidates, string[] idOrNameCandidates, string value)
        {
            foreach (var lab in labelCandidates)
            {
                var input = Page.GetByLabel(lab);
                if (await input.CountAsync() > 0)
                {
                    await input.FillAsync(value);
                    return;
                }
            }
            foreach (var sel in idOrNameCandidates)
            {
                var input = Page.Locator(sel);
                if (await input.CountAsync() > 0 && await input.First.IsVisibleAsync())
                {
                    await input.First.FillAsync(value);
                    return;
                }
            }
            Assert.Fail($"Nisam uspeo da popunim polje. Tražene labele: [{string.Join(", ", labelCandidates)}], selektori: [{string.Join(", ", idOrNameCandidates)}].");
        }

        [Test]
        public async Task Create_Museum_AppendOnly_VisibleInList()
        {
            var uniqueName = Unique("E2E Museum");
            var city = "Beograd";
            var desc = "Append-only E2E unos";
            await Page.GotoAsync($"{BaseUrl}/Muzeji");
            await ClickFirstButtonAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Dodaj novi", "Dodaj muzej");
            await FillByLabelOrIdAsync(
                new[] { "Naziv", "Name" },
                new[] { "#Name", "input[name='Name']" },
                uniqueName
            );
            await FillByLabelOrIdAsync(
                new[] { "Grad", "City" },
                new[] { "#City", "input[name='City']" },
                city
            );
            try
            {
                await FillByLabelOrIdAsync(
                    new[] { "Opis", "Description" },
                    new[] { "#Description", "textarea[name='Description']", "input[name='Description']" },
                    desc
                );
            }
            catch { }
            await ClickFirstButtonAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
            await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji.*"));
            var cell = Page.GetByRole(AriaRole.Cell, new() { Name = uniqueName });
            var found = false;
            for (int i = 0; i < 10; i++)
            {
                if (await cell.CountAsync() > 0 && await cell.First.IsVisibleAsync())
                {
                    found = true;
                    break;
                }
                await Page.ReloadAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            Assert.That(found, Is.True, $"Novi muzej '{uniqueName}' nije pronađen u tabeli nakon kreiranja.");
        }
    }
}
