using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MuseumTickets.Tests.E2E;

[NonParallelizable]
[Category("AppendOnlyE2E")]
public class MuseumsCreateE2ETests : PageTest
{
    private string BaseUrl => (Environment.GetEnvironmentVariable("E2E_BASEURL") ?? "http://localhost:7036").TrimEnd('/');
    private static string Unique(string prefix) => $"{prefix} {DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}".Substring(0, 40);
    private async Task OpenCreateFormAsync()
    {
        await Page.GotoAsync($"{BaseUrl}/Muzeji");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji.*", RegexOptions.IgnoreCase));
        var create = Page.GetByRole(AriaRole.Link, new() { Name = "+ Kreiraj" }).First;
        if (await create.CountAsync() == 0) create = Page.GetByText("+ Kreiraj", new() { Exact = false }).First;
        await create.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji/Kreiraj"));
    }

    private async Task FillByLabelAsync(string label, string value, bool required = true)
    {
        var el = Page.GetByLabel(label);
        if (await el.CountAsync() > 0)
        {
            await el.First.FillAsync(value);
        }
        else if (required)
        {
            Assert.Fail($"Polje sa etiketom '{label}' nije pronađeno.");
        }
    }

    private async Task ClickAnyAsync(params string[] labels)
    {
        foreach (var l in labels)
        {
            var btn = Page.GetByRole(AriaRole.Button, new() { Name = l });
            if (await btn.CountAsync() > 0 && await btn.First.IsVisibleAsync())
            {
                await btn.First.ClickAsync();
                return;
            }
        }
        foreach (var l in labels)
        {
            var link = Page.GetByRole(AriaRole.Link, new() { Name = l });
            if (await link.CountAsync() > 0 && await link.First.IsVisibleAsync())
            {
                await link.First.ClickAsync();
                return;
            }
        }
        foreach (var l in labels)
        {
            var txt = Page.GetByText(l, new() { Exact = false });
            if (await txt.CountAsync() > 0 && await txt.First.IsVisibleAsync())
            {
                await txt.First.ClickAsync();
                return;
            }
        }
        Assert.Fail($"Nije pronađeno dugme/link/tekst: {string.Join(", ", labels)}");
    }

    private async Task<bool> RowVisibleInIndexAsync(string museumName, int retries = 8, int delayMs = 700)
    {
        var cell = Page.GetByRole(AriaRole.Cell, new() { Name = museumName });
        for (int i = 0; i < retries; i++)
        {
            if (await cell.CountAsync() > 0 && await cell.First.IsVisibleAsync()) return true;
            await Page.ReloadAsync();
            await Page.WaitForTimeoutAsync(delayMs);
        }
        return false;
    }
    [Test]
    public async Task Create_Minimal_ShowsInIndex()
    {
        var name = Unique("E2E Muzej");
        var city = "Beograd";

        await OpenCreateFormAsync();
        await FillByLabelAsync("Naziv", name);
        await FillByLabelAsync("Grad", city);

        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji"));

        var found = await RowVisibleInIndexAsync(name);
        Assert.That(found, Is.True, $"Novi muzej '{name}' nije prikazan u listi nakon kreiranja.");
    }
    [Test]
    public async Task Create_WithLongDescription_ShowsInDetails()
    {
        var name = Unique("E2E Muzej (long)");
        var city = "Novi Sad";
        var longDesc = new string('x', 300);

        await OpenCreateFormAsync();
        await FillByLabelAsync("Naziv", name);
        await FillByLabelAsync("Grad", city);
        await FillByLabelAsync("Opis", longDesc, required: false);

        await ClickAnyAsync("Sačuvaj");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji"));

        var row = Page.GetByRole(AriaRole.Row, new() { Name = name });
        await Expect(row).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Link, new() { Name = "Detalji" }).ClickAsync();

        await Expect(Page.Locator("table")).ToBeVisibleAsync();
        await Expect(Page.GetByText(name, new() { Exact = false })).ToBeVisibleAsync();
        await Expect(Page.GetByText(city, new() { Exact = false })).ToBeVisibleAsync();
        await Expect(Page.GetByText(longDesc, new() { Exact = false })).ToBeVisibleAsync();
    }
    [Test]
    public async Task Create_Cancel_DoesNotCreate()
    {
        var name = Unique("E2E Muzej (cancel)");
        var city = "Niš";

        await OpenCreateFormAsync();
        await FillByLabelAsync("Naziv", name);
        await FillByLabelAsync("Grad", city);
        await FillByLabelAsync("Opis", "ne bi smelo da se sačuva", required: false);

        await ClickAnyAsync("Otkaži", "Odustani", "Nazad", "Cancel", "Back");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji"));

        var exists = await RowVisibleInIndexAsync(name, retries: 2, delayMs: 400);
        Assert.That(exists, Is.False, $"Muzej '{name}' je kreiran iako je kliknuto otkazivanje.");
    }
}
