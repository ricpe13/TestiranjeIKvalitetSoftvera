using System;
using System.Globalization;
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
    public class AppendOnlyTicketTypeE2ETests : PageTest
    {
        private string BaseUrl =>
            (Environment.GetEnvironmentVariable("E2E_BASEURL") ?? "http://localhost:7036").TrimEnd('/');

        private static string Unique(string prefix)
            => $"{prefix} {DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6]}";

        private static Random NewRandom() => new Random(Guid.NewGuid().GetHashCode());
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
            Assert.Fail($"Dugme nije pronađeno: {string.Join(", ", names)}");
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
            TestContext.WriteLine($"[WARN] Polje nije pronađeno (labels: {string.Join("/", labelCandidates)}). Preskačem.");
        }

        private async Task<bool> GoToTicketTypesListAsync()
        {
            await Page.GotoAsync($"{BaseUrl}/TipoviKarata");
            if (new Regex("/TipoviKarata", RegexOptions.IgnoreCase).IsMatch(Page.Url))
                return true;

            await Page.GotoAsync($"{BaseUrl}/TicketTypes");
            return new Regex("/TicketTypes", RegexOptions.IgnoreCase).IsMatch(Page.Url);
        }

        private async Task EnsureAtLeastOneMuseumExistsAsync()
        {
            await Page.GotoAsync($"{BaseUrl}/Muzeji");
            if (!new Regex("/Muzeji", RegexOptions.IgnoreCase).IsMatch(Page.Url))
                await Page.GotoAsync($"{BaseUrl}/Museums");

            var uniqueName = Unique("E2E Museum");
            await ClickFirstButtonAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
            await FillByLabelOrIdAsync(
                new[] { "Naziv", "Name" },
                new[] { "#Name", "input[name='Name']" },
                uniqueName
            );
            await FillByLabelOrIdAsync(
                new[] { "Grad", "City" },
                new[] { "#City", "input[name='City']" },
                "Beograd"
            );
            await FillByLabelOrIdAsync(
                new[] { "Opis", "Description" },
                new[] { "#Description", "textarea[name='Description']", "input[name='Description']" },
                "Auto-created for E2E"
            );
            await ClickFirstButtonAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
            await Expect(Page).ToHaveURLAsync(new Regex(".*/(Muzeji|Museums).*"));
        }

        private async Task SelectRandomMuseumAsync()
        {
            ILocator select = null!;
            string[] labelCandidates = { "Muzej", "Museum" };
            foreach (var lab in labelCandidates)
            {
                var s = Page.GetByLabel(lab);
                if (await s.CountAsync() > 0) { select = s.First; break; }
            }
            if (select is null)
            {
                var candidates = new[] { "select[name='MuseumId']", "#MuseumId", "select[id='MuseumId']" };
                foreach (var sel in candidates)
                {
                    var s = Page.Locator(sel);
                    if (await s.CountAsync() > 0 && await s.First.IsVisibleAsync()) { select = s.First; break; }
                }
            }
            Assert.NotNull(select, "Nije pronađen <select> za muzej.");

            var options = select.Locator("option");
            var count = await options.CountAsync();
            var valid = new System.Collections.Generic.List<(string value, string text)>();
            for (int i = 0; i < count; i++)
            {
                var val = (await options.Nth(i).GetAttributeAsync("value")) ?? "";
                if (!string.IsNullOrWhiteSpace(val))
                {
                    var txt = await options.Nth(i).TextContentAsync() ?? val;
                    valid.Add((val, txt.Trim()));
                }
            }

            if (valid.Count == 0)
            {
                TestContext.WriteLine("[INFO] Nema dostupnih muzeja. Kreiram jedan pa ponovo biram.");
                await EnsureAtLeastOneMuseumExistsAsync();
                await GoToTicketTypesListAsync();
                await ClickFirstButtonAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");

                await SelectRandomMuseumAsync();
                return;
            }

            var rnd = NewRandom();
            var pick = valid[rnd.Next(valid.Count)];
            await select.SelectOptionAsync(new[] { pick.value });
            TestContext.WriteLine($"[INFO] Izabran muzej: {pick.text} (value={pick.value})");
        }
        [Test]
        public async Task Create_TicketType_AppendOnly_RandomPrice_RandomMuseum()
        {
            var ok = await GoToTicketTypesListAsync();
            Assert.IsTrue(ok, "Nisam uspeo da otvorim listu (/TipoviKarata ili /TicketTypes).");
            await ClickFirstButtonAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
            await SelectRandomMuseumAsync();
            var uniqueName = Unique("E2E Ticket");
            await FillByLabelOrIdAsync(
                new[] { "Naziv", "Name" },
                new[] { "#Name", "input[name='Name']" },
                uniqueName
            );
            await FillByLabelOrIdAsync(
                new[] { "Opis", "Description" },
                new[] { "#Description", "textarea[name='Description']", "input[name='Description']" },
                "Append-only E2E tip karte"
            );
            var price = NewRandom().Next(10, 51);
            await FillByLabelOrIdAsync(
                new[] { "Cena", "Price" },
                new[] { "#Price", "input[name='Price']" },
                price.ToString(CultureInfo.InvariantCulture)
            );
            await ClickFirstButtonAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
            await Expect(Page).ToHaveURLAsync(new Regex(".*/(TipoviKarata|TicketTypes).*"));
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
            Assert.That(found, Is.True, $"Novi tip karte '{uniqueName}' nije pronađen u listi nakon kreiranja.");
        }
    }
}
