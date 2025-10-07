using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MuseumTickets.Tests.E2E;

[NonParallelizable]
[Category("AppendOnlyE2E")]
public class TicketTypesReadE2ETests : PageTest
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

    private async Task OpenTicketTypesIndexAsync()
    {
        await Page.GotoAsync($"{BaseUrl}/TipoviKarata");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/TipoviKarata.*", RegexOptions.IgnoreCase));
    }

    private async Task<bool> RowVisibleInIndexAsync(string ticketName, int retries = 8, int delayMs = 700)
    {
        var cell = Page.GetByRole(AriaRole.Cell, new() { Name = ticketName });
        for (int i = 0; i < retries; i++)
        {
            if (await cell.CountAsync() > 0 && await cell.First.IsVisibleAsync()) return true;
            await Page.ReloadAsync();
            await Page.WaitForTimeoutAsync(delayMs);
        }
        return false;
    }

    private async Task OpenDetailsFromRowAsync(string ticketName)
    {
        var row = Page.GetByRole(AriaRole.Row, new() { Name = ticketName });
        await Expect(row).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Link, new() { Name = "Detalji" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync(new Regex(".*/TipoviKarata/Detalji/\\d+"));
    }

    private async Task<string> EnsureMuseumAsync(string city = "Beograd")
    {
        var museumName = Unique("E2E Muzej za tip karte");
        await Page.GotoAsync($"{BaseUrl}/Muzeji");
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji/Kreiraj"));
        await FillByLabelAsync("Naziv", museumName);
        await FillByLabelAsync("Grad", city);
        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji"));
        return museumName;
    }

    private async Task CreateTicketTypeUIAsync(string name, string museumName, string price, string? desc = null)
    {
        await OpenTicketTypesIndexAsync();
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/TipoviKarata/Kreiraj"));

        await FillByLabelAsync("Naziv", name);
        await FillByLabelAsync("Cena", price);
        if (!string.IsNullOrWhiteSpace(desc)) await FillByLabelAsync("Opis", desc, required: false);
        await SelectOptionByContainsAsync(Page.GetByLabel("Muzej"), museumName);

        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/TipoviKarata"));
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
                Assert.That(string.IsNullOrWhiteSpace(txt), Is.False, "Detalji: vrednost za 'Muzej' je prazna.");
                return;
            }
        }
        var dd = Page.Locator("dd").First;
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
        var row = Page.Locator("tr").Filter(new() { HasTextString = label }).First;
        if (await row.CountAsync() > 0)
        {
            var td = row.Locator("td").First;
            if (await td.CountAsync() > 0)
            {
                var txt = (await td.InnerTextAsync())?.Trim();
                Assert.That(txt?.Contains(expectedSubstring) ?? false, Is.True,
                    $"{label}: očekuje se da sadrži '{expectedSubstring}', dobijeno '{txt}'.");
                return;
            }
        }
        var any = Page.GetByText(expectedSubstring, new() { Exact = false });
        Assert.That(await any.CountAsync() > 0, Is.True, $"{label}: '{expectedSubstring}' nije nađen na strani.");
        await Expect(any.First).ToBeVisibleAsync();
    }
    [Test]
    public async Task Index_Shows_Item_After_Reload()
    {
        var museum = await EnsureMuseumAsync();
        var ttName = Unique("READ Tip karte");
        await CreateTicketTypeUIAsync(ttName, museum, price: "120", desc: "read-check");

        await OpenTicketTypesIndexAsync();
        await Page.ReloadAsync();

        var found = await RowVisibleInIndexAsync(ttName);
        Assert.That(found, Is.True, $"Tip karte '{ttName}' nije vidljiv u listi posle reload-a.");
    }
    [Test]
    public async Task Details_Displays_Correct_Fields()
    {
        var museum = await EnsureMuseumAsync("Niš");
        var ttName = Unique("READ Tip (detalji)");
        var desc = "Opis za READ-Detalji validaciju.";
        const string price = "499";

        await CreateTicketTypeUIAsync(ttName, museum, price, desc);

        await OpenTicketTypesIndexAsync();
        await OpenDetailsFromRowAsync(ttName);

        await Expect(Page.Locator("table")).ToBeVisibleAsync();
        await Expect(Page.GetByText(ttName, new() { Exact = false })).ToBeVisibleAsync();
        await Expect(Page.GetByText(desc, new() { Exact = false })).ToBeVisibleAsync();
        await AssertFieldContainsAsync("Cena", price);
        await AssertMuseumShownOnDetailsAsync(museum);
    }
    [Test]
    public async Task Details_Back_Navigates_To_Index_And_Item_Remains()
    {
        var museum = await EnsureMuseumAsync("Novi Sad");
        var ttName = Unique("READ Tip (back)");

        await CreateTicketTypeUIAsync(ttName, museum, price: "250");

        await OpenTicketTypesIndexAsync();
        await OpenDetailsFromRowAsync(ttName);

        await ClickAnyAsync("Nazad", "Back");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/TipoviKarata"));

        var found = await RowVisibleInIndexAsync(ttName);
        Assert.That(found, Is.True, $"Tip karte '{ttName}' nije vidljiv u listi nakon povratka sa Detalja.");
    }
}
