using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MuseumTickets.Tests.E2E;

[NonParallelizable]
[Category("AppendOnlyE2E")]
public class TicketTypesUpdateE2ETests : PageTest
{
    private string BaseUrl => (Environment.GetEnvironmentVariable("E2E_BASEURL") ?? "http://localhost:7036").TrimEnd('/');
    private static string Unique(string prefix) => $"{prefix} {DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}".Substring(0, 40);
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

    private async Task OpenTicketTypesIndexAsync()
    {
        await Page.GotoAsync($"{BaseUrl}/TipoviKarata");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task<bool> RowVisibleInIndexAsync(string ticketName, int retries = 8, int delayMs = 700)
    {
        var row = Page.Locator("tbody tr").Filter(new() { HasTextString = ticketName }).First;
        for (int i = 0; i < retries; i++)
        {
            if (await row.CountAsync() > 0 && await row.IsVisibleAsync()) return true;
            await Page.ReloadAsync();
            await Page.WaitForTimeoutAsync(delayMs);
        }
        return false;
    }

    private async Task OpenDetailsFromRowAsync(string ticketName)
    {
        await OpenTicketTypesIndexAsync();
        var found = await RowVisibleInIndexAsync(ticketName);
        Assert.That(found, Is.True, $"Na listi Tipova karata nije pronađen red sa nazivom '{ticketName}'.");
        var row = Page.Locator("tbody tr").Filter(new() { HasTextString = ticketName }).First;
        var link = row.GetByRole(AriaRole.Link, new() { Name = "Detalji" });
        if (await link.CountAsync() == 0) link = row.GetByText("Detalji", new() { Exact = false });
        await link.First.ClickAsync();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync(new Regex(".*/TipoviKarata/Detalji/\\d+$"));
    }

    private async Task OpenEditForAsync(string ticketName)
    {
        await OpenTicketTypesIndexAsync();
        var found = await RowVisibleInIndexAsync(ticketName);
        Assert.That(found, Is.True, $"Na listi Tipova karata nije pronađen red sa nazivom '{ticketName}'.");

        var row = Page.Locator("tbody tr").Filter(new() { HasTextString = ticketName }).First;
        var edit = row.GetByRole(AriaRole.Link, new() { Name = "Izmeni" });
        if (await edit.CountAsync() == 0) edit = row.GetByText("Izmeni", new() { Exact = false });
        await edit.First.ClickAsync();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync(new Regex(".*/TipoviKarata/Izmeni/\\d+$"));
    }

    private async Task<string> EnsureMuseumAsync(string city = "Beograd")
    {
        var museumName = Unique("E2E Muzej za tip");
        await Page.GotoAsync($"{BaseUrl}/Muzeji");
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji/Kreiraj$"));
        await FillByLabelAsync("Naziv", museumName);
        await FillByLabelAsync("Grad", city);
        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return museumName;
    }

    private async Task CreateTicketTypeUIAsync(string name, string museumName, string price, string? desc = null)
    {
        await OpenTicketTypesIndexAsync();
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/TipoviKarata/Kreiraj$"));

        await FillByLabelAsync("Naziv", name);
        await FillByLabelAsync("Cena", price);
        if (!string.IsNullOrWhiteSpace(desc)) await FillByLabelAsync("Opis", desc, required: false);
        await SelectOptionByContainsAsync(Page.GetByLabel("Muzej"), museumName);

        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await RowVisibleInIndexAsync(name);
    }

    private async Task AssertMuseumShownOnDetailsAsync(string? expectedMuseumName = null)
    {
        if (!string.IsNullOrWhiteSpace(expectedMuseumName))
        {
            var direct = Page.GetByText(expectedMuseumName, new() { Exact = false });
            if (await direct.CountAsync() > 0) { await Expect(direct.First).ToBeVisibleAsync(); return; }
        }
        var row = Page.Locator("tr:has(th), tr:has(td)");
        var museumRow = row.Filter(new() { HasTextString = "Muzej" }).First;
        if (await museumRow.CountAsync() > 0)
        {
            var val = museumRow.Locator("td").Last;
            if (await val.CountAsync() > 0)
            {
                var txt = (await val.InnerTextAsync())?.Trim();
                Assert.That(string.IsNullOrWhiteSpace(txt), Is.False, "Detalji: vrednost za 'Muzej' je prazna.");
                return;
            }
        }
        var dd = Page.Locator("dt:has-text(\"Muzej\") + dd").First;
        if (await dd.CountAsync() > 0)
        {
            var txt = (await dd.InnerTextAsync())?.Trim();
            Assert.That(string.IsNullOrWhiteSpace(txt), Is.False, "Detalji: nema prikaza povezanog muzeja.");
            return;
        }
        Assert.Fail("Detalji: nije moguće potvrditi prikaz povezanog muzeja.");
    }

    private async Task AssertFieldContainsAsync(string label, string expectedSubstring)
    {
        var tableValue = Page.Locator($"tr:has(th:has-text(\"{label}\")) td, tr:has(td:first-child:has-text(\"{label}\")) td:nth-child(2)").First;
        if (await tableValue.CountAsync() > 0)
        {
            var txt = (await tableValue.InnerTextAsync())?.Trim() ?? "";
            Assert.That(txt.Contains(expectedSubstring), Is.True,
                $"{label}: očekuje se da sadrži '{expectedSubstring}', dobijeno '{txt}'.");
            return;
        }
        var dlValue = Page.Locator($"dt:has-text(\"{label}\") + dd").First;
        if (await dlValue.CountAsync() > 0)
        {
            var txt = (await dlValue.InnerTextAsync())?.Trim() ?? "";
            Assert.That(txt.Contains(expectedSubstring), Is.True,
                $"{label}: očekuje se da sadrži '{expectedSubstring}', dobijeno '{txt}'.");
            return;
        }
        var any = Page.GetByText(expectedSubstring, new() { Exact = false });
        Assert.That(await any.CountAsync() > 0, Is.True, $"{label}: '{expectedSubstring}' nije nađen na strani.");
        await Expect(any.First).ToBeVisibleAsync();
    }

    [Test]
    public async Task Edit_ChangePrice_Saves()
    {
        var museum = await EnsureMuseumAsync();
        var ttName = Unique("UPD Tip (cena)");

        await CreateTicketTypeUIAsync(ttName, museum, price: "120", desc: "pre izmene");

        await OpenEditForAsync(ttName);
        await FillByLabelAsync("Cena", "799");
        await ClickAnyAsync("Sačuvaj izmene", "Sačuvaj", "Snimi", "Save", "Update");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await OpenDetailsFromRowAsync(ttName);
        await AssertFieldContainsAsync("Cena", "799");
    }

    [Test]
    public async Task Edit_Cancel_DoesNotSave()
    {
        var museum = await EnsureMuseumAsync("Niš");
        var ttName = Unique("UPD Tip (cancel)");

        await CreateTicketTypeUIAsync(ttName, museum, price: "250", desc: "ostaje stara cena");

        await OpenEditForAsync(ttName);
        await FillByLabelAsync("Cena", "999");
        await ClickAnyAsync("Otkaži", "Odustani", "Nazad", "Cancel", "Back");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await OpenDetailsFromRowAsync(ttName);
        await AssertFieldContainsAsync("Cena", "250");
    }

    [Test]
    public async Task Edit_ChangeMuseum_Saves_And_ShowsInDetails()
    {
        var museumA = await EnsureMuseumAsync("Kragujevac");
        var museumB = await EnsureMuseumAsync("Subotica");
        var ttName = Unique("UPD Tip (muzej)");

        await CreateTicketTypeUIAsync(ttName, museumA, price: "300", desc: "pre promene muzeja");

        await OpenEditForAsync(ttName);
        await SelectOptionByContainsAsync(Page.GetByLabel("Muzej"), museumB);
        await ClickAnyAsync("Sačuvaj izmene", "Sačuvaj", "Snimi", "Save", "Update");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await OpenDetailsFromRowAsync(ttName);
        await AssertMuseumShownOnDetailsAsync(museumB);
    }
}
