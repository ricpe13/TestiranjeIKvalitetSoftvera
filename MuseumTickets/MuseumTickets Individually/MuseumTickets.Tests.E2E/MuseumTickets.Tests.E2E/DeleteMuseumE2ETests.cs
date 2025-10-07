using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MuseumTickets.Tests.E2E
{
    [TestFixture]
    [Category("DestructiveE2E")]
    [NonParallelizable]
    public class DeleteMuseumE2ETests : PageTest
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
                var link = Page.GetByRole(AriaRole.Link, new() { Name = n });
                if (await link.CountAsync() > 0 && await link.First.IsVisibleAsync())
                {
                    await link.First.ClickAsync();
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
            Assert.Fail($"Nisam našao dugme/link: {string.Join(", ", names)}");
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
            Assert.Fail($"Polje nije pronađeno (labels: {string.Join("/", labelCandidates)}; selektori: {string.Join("/", idOrNameCandidates)})");
        }

        private async Task GoToMuseumsAsync()
        {
            await Page.GotoAsync($"{BaseUrl}/Muzeji");
            if (!new Regex("/Muzeji", RegexOptions.IgnoreCase).IsMatch(Page.Url))
                await Page.GotoAsync($"{BaseUrl}/Museums");

            Assert.IsTrue(
                new Regex("/(Muzeji|Museums)", RegexOptions.IgnoreCase).IsMatch(Page.Url),
                "Nisam uspeo da otvorim listu muzeja (/Muzeji ili /Museums)."
            );
        }

        private async Task CreateMuseumUIAsync(string name, string city, string? desc = null)
        {
            await ClickFirstButtonAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
            await FillByLabelOrIdAsync(new[] { "Naziv", "Name" }, new[] { "#Name", "input[name='Name']" }, name);
            await FillByLabelOrIdAsync(new[] { "Grad", "City" }, new[] { "#City", "input[name='City']" }, city);

            if (!string.IsNullOrWhiteSpace(desc))
            {
                try
                {
                    await FillByLabelOrIdAsync(
                        new[] { "Opis", "Description" },
                        new[] { "#Description", "textarea[name='Description']", "input[name='Description']" },
                        desc
                    );
                }
                catch { }
            }

            await ClickFirstButtonAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
            await Expect(Page).ToHaveURLAsync(new Regex(".*/(Muzeji|Museums).*"));
            var cell = Page.GetByRole(AriaRole.Cell, new() { Name = name });
            for (int i = 0; i < 10; i++)
            {
                if (await cell.CountAsync() > 0 && await cell.First.IsVisibleAsync()) return;
                await Page.ReloadAsync();
                await Page.WaitForTimeoutAsync(1000);
            }
            Assert.Fail($"Kreirani muzej '{name}' se ne vidi u tabeli.");
        }

        private async Task DeleteMuseumUIAsync(string name)
        {
            var row = Page.GetByRole(AriaRole.Row, new() { Name = name });
            Assert.That(await row.CountAsync(), Is.GreaterThan(0), $"Red sa '{name}' nije pronađen.");
            var delBtn = row.GetByRole(AriaRole.Button, new() { Name = "Obriši" });
            var delLnk = row.GetByRole(AriaRole.Link, new() { Name = "Obriši" });

            if (await delBtn.CountAsync() > 0 && await delBtn.First.IsVisibleAsync())
            {
                await delBtn.First.ClickAsync();
            }
            else if (await delLnk.CountAsync() > 0 && await delLnk.First.IsVisibleAsync())
            {
                await delLnk.First.ClickAsync();
            }
            else
            {
                delBtn = row.GetByRole(AriaRole.Button, new() { Name = "Delete" });
                delLnk = row.GetByRole(AriaRole.Link, new() { Name = "Delete" });
                if (await delBtn.CountAsync() > 0 && await delBtn.First.IsVisibleAsync())
                    await delBtn.First.ClickAsync();
                else if (await delLnk.CountAsync() > 0 && await delLnk.First.IsVisibleAsync())
                    await delLnk.First.ClickAsync();
                else
                    Assert.Fail("Nisam našao akciju za brisanje u redu.");
            }
            await ClickFirstButtonAsync("Obriši", "Delete", "Potvrdi", "Confirm");
            await Expect(Page).ToHaveURLAsync(new Regex(".*/(Muzeji|Museums).*"));
        }

        private async Task AssertMuseumNotInListAsync(string name)
        {
            var cell = Page.GetByRole(AriaRole.Cell, new() { Name = name });
            for (int i = 0; i < 5; i++)
            {
                if (await cell.CountAsync() == 0)
                    return;
                if (await cell.First.IsVisibleAsync() == false)
                    return;

                await Page.ReloadAsync();
                await Page.WaitForTimeoutAsync(500);
            }
            Assert.Fail($"Muzej '{name}' je i dalje vidljiv posle brisanja.");
        }
        [Test]
        public async Task Create_Then_Delete_Museum_Disappears_From_List()
        {
            var uniqueName = Unique("E2E DEL Museum");
            await GoToMuseumsAsync();
            await CreateMuseumUIAsync(uniqueName, city: "Beograd", desc: "E2E delete test");
            await DeleteMuseumUIAsync(uniqueName);
            await AssertMuseumNotInListAsync(uniqueName);
        }
    }
}
