using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MuseumTickets.Tests.E2E;

[NonParallelizable]
[Category("AppendOnlyE2E")]
public class ExhibitionsReadE2ETests : PageTest
{
    private string BaseUrl => (Environment.GetEnvironmentVariable("E2E_BASEURL") ?? "http://localhost:7036").TrimEnd('/');
    private static string Unique(string prefix) => $"{prefix} {DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}".Substring(0, 40);
    private async Task OpenExhibitionsIndexAsync()
    {
        await Page.GotoAsync($"{BaseUrl}/Izlozbe");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe.*", RegexOptions.IgnoreCase));
    }

    private async Task ClickAnyAsync(params string[] labels)
    {
        foreach (var l in labels)
        {
            var btn = Page.GetByRole(AriaRole.Button, new() { Name = l });
            if (await btn.CountAsync() > 0 && await btn.First.IsVisibleAsync()) { await btn.First.ClickAsync(); return; }
        }
        foreach (var l in labels)
        {
            var link = Page.GetByRole(AriaRole.Link, new() { Name = l });
            if (await link.CountAsync() > 0 && await link.First.IsVisibleAsync()) { await link.First.ClickAsync(); return; }
        }
        foreach (var l in labels)
        {
            var txt = Page.GetByText(l, new() { Exact = false });
            if (await txt.CountAsync() > 0 && await txt.First.IsVisibleAsync()) { await txt.First.ClickAsync(); return; }
        }
        Assert.Fail($"Nije pronađeno dugme/link/tekst: {string.Join(", ", labels)}");
    }

    private async Task FillByLabelAsync(string label, string value, bool required = true)
    {
        var el = Page.GetByLabel(label);
        if (await el.CountAsync() > 0) await el.First.FillAsync(value);
        else if (required) Assert.Fail($"Polje sa etiketom '{label}' nije pronađeno.");
    }

    private static async Task SelectOptionByContainsAsync(ILocator select, string containsText)
    {
        try { await select.SelectOptionAsync(new SelectOptionValue { Label = containsText }); return; } catch { }
        var opt = select.Locator("option").Filter(new() { HasTextString = containsText }).First;
        if (await opt.CountAsync() == 0) Assert.Fail($"Opcija koja sadrži '{containsText}' nije pronađena u <select>.");
        var val = await opt.GetAttributeAsync("value");
        await select.SelectOptionAsync(val);
    }

    private static (string start, string end) DefaultDates()
    {
        var s = DateTime.UtcNow.Date;
        var e = s.AddDays(7);
        return (s.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                e.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    private async Task<string> EnsureMuseumAsync(string city = "Beograd")
    {
        var museumName = Unique("E2E Muzej za izložbu");
        await Page.GotoAsync($"{BaseUrl}/Muzeji");
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji/Kreiraj"));
        await FillByLabelAsync("Naziv", museumName);
        await FillByLabelAsync("Grad", city);
        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji"));
        return museumName;
    }

    private async Task CreateExhibitionUIAsync(string title, string museumName, string? desc = null)
    {
        var (start, end) = DefaultDates();
        await OpenExhibitionsIndexAsync();
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe/Kreiraj"));

        await FillByLabelAsync("Naziv", title);
        await FillByLabelAsync("Datum početka", start);
        await FillByLabelAsync("Datum završetka", end);
        if (!string.IsNullOrWhiteSpace(desc)) await FillByLabelAsync("Opis", desc, required: false);
        await SelectOptionByContainsAsync(Page.GetByLabel("Muzej"), museumName);

        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe"));
    }

    private async Task<bool> RowVisibleInIndexAsync(string exhibitionTitle, int retries = 8, int delayMs = 700)
    {
        var cell = Page.GetByRole(AriaRole.Cell, new() { Name = exhibitionTitle });
        for (int i = 0; i < retries; i++)
        {
            if (await cell.CountAsync() > 0 && await cell.First.IsVisibleAsync()) return true;
            await Page.ReloadAsync();
            await Page.WaitForTimeoutAsync(delayMs);
        }
        return false;
    }

    private async Task OpenDetailsFromRowAsync(string exhibitionTitle)
    {
        var row = Page.GetByRole(AriaRole.Row, new() { Name = exhibitionTitle });
        await Expect(row).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Link, new() { Name = "Detalji" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe/Detalji/\\d+"));
    }

    private async Task AssertMuseumShownOnDetailsAsync(string? expectedMuseumName = null)
    {
        if (!string.IsNullOrWhiteSpace(expectedMuseumName))
        {
            var direct = Page.GetByText(expectedMuseumName, new() { Exact = false });
            if (await direct.CountAsync() > 0) { await Expect(direct.First).ToBeVisibleAsync(); return; }
        }
        var row = Page.Locator("tr").Filter(new() { HasTextString = "Muzej" }).First;
        if (await row.CountAsync() > 0)
        {
            var td = row.Locator("td").First;
            if (await td.CountAsync() > 0)
            {
                var txt = (await td.InnerTextAsync())?.Trim();
                Assert.That(string.IsNullOrWhiteSpace(txt), Is.False, "Detalji izložbe: vrednost za 'Muzej' je prazna.");
                return;
            }
        }
        var dd = Page.Locator("dd").First;
        if (await dd.CountAsync() > 0)
        {
            var txt = (await dd.InnerTextAsync())?.Trim();
            Assert.That(string.IsNullOrWhiteSpace(txt), Is.False, "Detalji izložbe: nema prikaza povezanog muzeja.");
            return;
        }
        Assert.Fail("Detalji izložbe: nije moguće potvrditi prikaz povezanog muzeja.");
    }
    [Test]
    public async Task Index_Shows_Item_After_Reload()
    {
        var museumName = await EnsureMuseumAsync();
        var title = Unique("READ Izložba");
        await CreateExhibitionUIAsync(title, museumName, "read-check");

        await OpenExhibitionsIndexAsync();
        await Page.ReloadAsync();

        var found = await RowVisibleInIndexAsync(title);
        Assert.That(found, Is.True, $"Izložba '{title}' nije vidljiva u listi posle reload-a.");
    }
    [Test]
    public async Task Details_Displays_Correct_Fields()
    {
        var museumName = await EnsureMuseumAsync("Niš");
        var title = Unique("READ Izložba (detalji)");
        var longDesc = "Opis za READ-Detalji verifikaciju.";

        await CreateExhibitionUIAsync(title, museumName, longDesc);

        await OpenExhibitionsIndexAsync();
        await OpenDetailsFromRowAsync(title);

        await Expect(Page.Locator("table")).ToBeVisibleAsync();
        await Expect(Page.GetByText(title, new() { Exact = false })).ToBeVisibleAsync();
        await Expect(Page.GetByText(longDesc, new() { Exact = false })).ToBeVisibleAsync();
        await AssertMuseumShownOnDetailsAsync(museumName);
    }
    [Test]
    public async Task Details_Back_Navigates_To_Index_And_Item_Remains()
    {
        var museumName = await EnsureMuseumAsync("Novi Sad");
        var title = Unique("READ Izložba (back)");

        await CreateExhibitionUIAsync(title, museumName);

        await OpenExhibitionsIndexAsync();
        await OpenDetailsFromRowAsync(title);

        await ClickAnyAsync("Nazad", "Back");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe"));

        var found = await RowVisibleInIndexAsync(title);
        Assert.That(found, Is.True, $"Izložba '{title}' nije vidljiva u listi nakon povratka sa Detalja.");
    }
}
