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
public class ExhibitionsUpdateE2ETests : PageTest
{
    private string BaseUrl => (Environment.GetEnvironmentVariable("E2E_BASEURL") ?? "http://localhost:7036").TrimEnd('/');
    private static string Unique(string prefix) => $"{prefix} {DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}".Substring(0, 40);
    private async Task ClickAnyAsync(params string[] labels)
    {
        foreach (var l in labels)
        {
            var btn = Page.GetByRole(AriaRole.Button, new() { Name = l });
            if (await btn.CountAsync() > 0 && await btn.First.IsVisibleAsync())
            { await btn.First.ClickAsync(); return; }
        }
        foreach (var l in labels)
        {
            var link = Page.GetByRole(AriaRole.Link, new() { Name = l });
            if (await link.CountAsync() > 0 && await link.First.IsVisibleAsync())
            { await link.First.ClickAsync(); return; }
        }
        foreach (var l in labels)
        {
            var txt = Page.GetByText(l, new() { Exact = false });
            if (await txt.CountAsync() > 0 && await txt.First.IsVisibleAsync())
            { await txt.First.ClickAsync(); return; }
        }
        Assert.Fail($"Nije pronađeno dugme/link/tekst: {string.Join(", ", labels)}");
    }

    private async Task FillByLabelAsync(string label, string value, bool required = true)
    {
        var el = Page.GetByLabel(label);
        if (await el.CountAsync() > 0) { await el.First.FillAsync(value); }
        else if (required) { Assert.Fail($"Polje sa etiketom '{label}' nije pronađeno."); }
    }

    private static async Task SelectOptionByContainsAsync(ILocator select, string containsText)
    {
        try { await select.SelectOptionAsync(new SelectOptionValue { Label = containsText }); return; } catch { }
        var opt = select.Locator("option").Filter(new() { HasTextString = containsText }).First;
        if (await opt.CountAsync() == 0) Assert.Fail($"Opcija koja sadrži '{containsText}' nije pronađena u <select>.");
        var val = await opt.GetAttributeAsync("value");
        await select.SelectOptionAsync(val);
    }

    private async Task OpenExhibitionsIndexAsync()
    {
        await Page.GotoAsync($"{BaseUrl}/Izlozbe");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe.*", RegexOptions.IgnoreCase));
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

    private async Task OpenEditForAsync(string exhibitionTitle)
    {
        await OpenExhibitionsIndexAsync();
        var row = Page.GetByRole(AriaRole.Row, new() { Name = exhibitionTitle });
        await Expect(row).ToBeVisibleAsync();
        var edit = row.GetByRole(AriaRole.Link, new() { Name = "Izmeni" });
        if (await edit.CountAsync() == 0) edit = row.GetByRole(AriaRole.Button, new() { Name = "Izmeni" });
        await edit.First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe/Izmeni/\\d+"));
    }

    private async Task OpenDetailsFromRowAsync(string exhibitionTitle)
    {
        var row = Page.GetByRole(AriaRole.Row, new() { Name = exhibitionTitle });
        await Expect(row).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Link, new() { Name = "Detalji" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe/Detalji/\\d+"));
    }
    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd", "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yyyy.", "d.M.yyyy.",
        "MM/dd/yyyy", "M/d/yyyy", "dd/MM/yyyy", "d/M/yyyy"
    };

    private string[] DateCandidates(DateTime d) => new[]
    {
        d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        d.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
        d.ToString("d.M.yyyy", CultureInfo.InvariantCulture),
        d.ToString("dd.MM.yyyy.", CultureInfo.InvariantCulture),
        d.ToString("d.M.yyyy.", CultureInfo.InvariantCulture),
        d.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture),
        d.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
        d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        d.ToString("d/M/yyyy", CultureInfo.InvariantCulture)
    };

    private async Task AssertDateFieldOnDetailsAsync(string label, DateTime expectedDate)
    {
        var row = Page.Locator("tr").Filter(new() { HasTextString = label }).First;
        if (await row.CountAsync() > 0)
        {
            var td = row.Locator("td").First;
            if (await td.CountAsync() > 0)
            {
                var txt = (await td.InnerTextAsync())?.Trim();
                Assert.That(string.IsNullOrWhiteSpace(txt), Is.False, $"{label}: vrednost je prazna.");
                if (DateTime.TryParseExact(txt, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ||
                    DateTime.TryParse(txt, CultureInfo.GetCultureInfo("sr-RS"), DateTimeStyles.None, out parsed) ||
                    DateTime.TryParse(txt, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                {
                    Assert.That(parsed.Date, Is.EqualTo(expectedDate.Date),
                        $"{label}: dobijeno '{txt}', očekivano '{expectedDate:yyyy-MM-dd}'.");
                    return;
                }
                foreach (var cand in DateCandidates(expectedDate))
                    if (txt.Contains(cand))
                        return;

                Assert.Fail($"{label}: ne mogu da potvrdim datum. Dobijeno '{txt}', očekivano oko '{expectedDate:yyyy-MM-dd}'.");
            }
        }
        foreach (var cand in DateCandidates(expectedDate))
        {
            var loc = Page.GetByText(cand, new() { Exact = false });
            if (await loc.CountAsync() > 0) { await Expect(loc.First).ToBeVisibleAsync(); return; }
        }

        Assert.Fail($"{label}: nije nađen prikaz datuma na stranici Detalja.");
    }
    [Test]
    public async Task Edit_ChangeDescription_Saves_And_ShowsInDetails()
    {
        var museum = await EnsureMuseumAsync();
        var title = Unique("UPD Izložba (opis)");
        var initialDesc = "pre update opis";

        await CreateExhibitionUIAsync(title, museum, initialDesc);

        await OpenEditForAsync(title);
        var longDesc = new string('x', 320);
        await FillByLabelAsync("Opis", longDesc, required: false);
        await ClickAnyAsync("Sačuvaj izmene", "Sačuvaj", "Snimi", "Save", "Update");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe"));

        await OpenDetailsFromRowAsync(title);
        await Expect(Page.GetByText(longDesc, new() { Exact = false })).ToBeVisibleAsync();
    }
    [Test]
    public async Task Edit_Cancel_DoesNotSave()
    {
        var museum = await EnsureMuseumAsync("Niš");
        var title = Unique("UPD Izložba (cancel)");
        var initialDesc = "opis koji treba da ostane";

        await CreateExhibitionUIAsync(title, museum, initialDesc);

        await OpenEditForAsync(title);
        await FillByLabelAsync("Opis", "ovo NE SME da se sačuva", required: false);
        await ClickAnyAsync("Otkaži", "Odustani", "Nazad", "Cancel", "Back");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe"));

        await OpenDetailsFromRowAsync(title);
        await Expect(Page.GetByText(initialDesc, new() { Exact = false })).ToBeVisibleAsync();
    }
    [Test]
    public async Task Edit_ChangeDates_Saves_And_ShowsInDetails()
    {
        var museum = await EnsureMuseumAsync("Novi Sad");
        var title = Unique("UPD Izložba (datumi)");

        await CreateExhibitionUIAsync(title, museum, desc: "datumi pre izmene");
        var newStart = DateTime.UtcNow.Date.AddDays(1);
        var newEnd = newStart.AddDays(10);
        var newStartStr = newStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var newEndStr = newEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        await OpenEditForAsync(title);
        await FillByLabelAsync("Datum početka", newStartStr);
        await FillByLabelAsync("Datum završetka", newEndStr);
        await ClickAnyAsync("Sačuvaj izmene", "Sačuvaj", "Snimi", "Save", "Update");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe"));

        await OpenDetailsFromRowAsync(title);
        await AssertDateFieldOnDetailsAsync("Datum početka", newStart);
        await AssertDateFieldOnDetailsAsync("Datum završetka", newEnd);
    }
}
