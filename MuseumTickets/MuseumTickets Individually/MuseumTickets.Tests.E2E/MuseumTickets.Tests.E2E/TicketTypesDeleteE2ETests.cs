using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MuseumTickets.Tests.E2E;

[NonParallelizable]
[Category("DestructiveE2E")]
public class TicketTypesDeleteE2ETests : PageTest
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

    private async Task OpenDeleteForAsync(string ticketName)
    {
        await OpenTicketTypesIndexAsync();
        var row = Page.Locator("tbody tr").Filter(new() { HasTextString = ticketName }).First;
        Assert.That(await row.CountAsync() > 0, Is.True, $"Red sa '{ticketName}' nije pronađen u listi.");

        var del = row.GetByRole(AriaRole.Link, new() { Name = "Obriši" });
        if (await del.CountAsync() == 0) del = row.GetByText("Obriši", new() { Exact = false });
        await del.First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task<string> EnsureMuseumAsync(string city = "Beograd")
    {
        var museumName = Unique("E2E Muzej za tip");
        await Page.GotoAsync($"{BaseUrl}/Muzeji");
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji/Kreiraj"));
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
        await Expect(Page).ToHaveURLAsync(new Regex(".*/TipoviKarata/Kreiraj"));

        await FillByLabelAsync("Naziv", name);
        await FillByLabelAsync("Cena", price);
        if (!string.IsNullOrWhiteSpace(desc)) await FillByLabelAsync("Opis", desc, required: false);
        await SelectOptionByContainsAsync(Page.GetByLabel("Muzej"), museumName);

        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await RowVisibleInIndexAsync(name);
    }

    private async Task CreateExhibitionForAsync(string museumName, string exhibitionTitle)
    {
        await Page.GotoAsync($"{BaseUrl}/Izlozbe");
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe/Kreiraj"));

        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(7);

        await FillByLabelAsync("Naziv", exhibitionTitle);
        await FillByLabelAsync("Datum početka", start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        await FillByLabelAsync("Datum završetka", end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        await FillByLabelAsync("Opis", "E2E dependent – exhibition", required: false);
        await SelectOptionByContainsAsync(Page.GetByLabel("Muzej"), museumName);

        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task CreateOrderUsingAsync(string ticketName, string exhibitionTitle)
    {
        await Page.GotoAsync($"{BaseUrl}/Porudzbine");
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Porudzbine/Kreiraj"));

        await FillByLabelAsync("Ime kupca", Unique("Kupac"));
        await FillByLabelAsync("Email", $"test+{DateTime.UtcNow.Ticks}@example.com");
        await FillByLabelAsync("Količina", "2");

        var ticketSelect = Page.GetByLabel("Tip karte");
        await SelectOptionByContainsAsync(ticketSelect, ticketName);

        var exibSelect = Page.GetByLabel("Izložba");
        await SelectOptionByContainsAsync(exibSelect, exhibitionTitle);

        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    [Test]
    public async Task Delete_Cancel_DoesNotRemove()
    {
        var museum = await EnsureMuseumAsync();
        var ttName = Unique("DEL Tip (cancel)");
        await CreateTicketTypeUIAsync(ttName, museum, price: "120");

        await OpenDeleteForAsync(ttName);
        await ClickAnyAsync("Odustani", "Otkaži", "Nazad", "Cancel", "Back");
        await OpenTicketTypesIndexAsync();

        var stillHere = await RowVisibleInIndexAsync(ttName);
        Assert.That(stillHere, Is.True, $"Tip karte '{ttName}' je nestao iako je kliknuto otkazivanje.");
    }
    [Test]
    public async Task Delete_Succeeds_When_No_Dependents()
    {
        var museum = await EnsureMuseumAsync("Niš");
        var ttName = Unique("DEL Tip (ok)");
        await CreateTicketTypeUIAsync(ttName, museum, price: "250");

        await OpenDeleteForAsync(ttName);
        await ClickAnyAsync("Obriši", "Delete", "Confirm");
        await OpenTicketTypesIndexAsync();

        var present = await RowVisibleInIndexAsync(ttName, retries: 3, delayMs: 500);
        Assert.That(present, Is.False, $"Tip karte '{ttName}' i dalje postoji nakon uspešnog brisanja.");
    }
    [Test]
    public async Task Delete_Prevented_When_Order_Depends_On_TicketType()
    {
        var museum = await EnsureMuseumAsync("Novi Sad");
        var ttName = Unique("DEL Tip (blocked)");
        var exibTitle = Unique("DEL Izložba za TT");

        await CreateTicketTypeUIAsync(ttName, museum, price: "300");
        await CreateExhibitionForAsync(museum, exibTitle);
        await CreateOrderUsingAsync(ttName, exibTitle);

        await OpenDeleteForAsync(ttName);
        await ClickAnyAsync("Obriši", "Delete", "Confirm");
        try
        {
            var alert = Page.Locator(".alert-danger, .alert-warning, .validation-summary-errors");
            if (await alert.CountAsync() > 0) await Expect(alert.First).ToBeVisibleAsync();
        }
        catch { }

        await OpenTicketTypesIndexAsync();
        var stillHere = await RowVisibleInIndexAsync(ttName);
        Assert.That(stillHere, Is.True, $"Tip karte '{ttName}' je obrisan iako postoji zavisna porudžbina.");
    }
}
