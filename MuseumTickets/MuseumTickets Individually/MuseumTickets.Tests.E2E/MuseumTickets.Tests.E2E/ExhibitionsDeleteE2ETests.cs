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
public class ExhibitionsDeleteE2ETests : PageTest
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
        if (await opt.CountAsync() == 0)
            Assert.Fail($"Opcija koja sadrži '{containsText}' nije pronađena u <select>.");
        var val = await opt.GetAttributeAsync("value");
        await select.SelectOptionAsync(val);
    }

    private async Task OpenExhibitionsIndexAsync()
    {
        await Page.GotoAsync($"{BaseUrl}/Izlozbe");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe.*", RegexOptions.IgnoreCase));
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

    private async Task OpenDeleteForAsync(string exhibitionTitle)
    {
        await OpenExhibitionsIndexAsync();
        var row = Page.GetByRole(AriaRole.Row, new() { Name = exhibitionTitle });
        await Expect(row).ToBeVisibleAsync();
        var del = row.GetByRole(AriaRole.Link, new() { Name = "Obriši" });
        if (await del.CountAsync() == 0) del = row.GetByRole(AriaRole.Button, new() { Name = "Obriši" });
        await del.First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe/Obrisi/\\d+"));
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
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(7);
        await OpenExhibitionsIndexAsync();
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe/Kreiraj"));

        await FillByLabelAsync("Naziv", title);
        await FillByLabelAsync("Datum početka", start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        await FillByLabelAsync("Datum završetka", end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(desc)) await FillByLabelAsync("Opis", desc, required: false);
        await SelectOptionByContainsAsync(Page.GetByLabel("Muzej"), museumName);

        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe"));
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = title })).ToBeVisibleAsync();
    }

    private async Task CreateTicketTypeForAsync(string museumName, string ticketName)
    {
        await Page.GotoAsync($"{BaseUrl}/TipoviKarata");
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/TipoviKarata/Kreiraj"));

        await FillByLabelAsync("Naziv", ticketName);
        await FillByLabelAsync("Cena", "25");
        await FillByLabelAsync("Opis", "E2E dependent – ticket", required: false);
        await SelectOptionByContainsAsync(Page.GetByLabel("Muzej"), museumName);

        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/TipoviKarata"));
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
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Porudzbine"));
    }
    [Test]
    public async Task Delete_Cancel_DoesNotRemove()
    {
        var museum = await EnsureMuseumAsync();
        var title = Unique("DEL Izložba (cancel)");
        await CreateExhibitionUIAsync(title, museum);

        await OpenDeleteForAsync(title);
        await ClickAnyAsync("Odustani", "Otkaži", "Nazad", "Cancel", "Back");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe"));

        var stillHere = await RowVisibleInIndexAsync(title);
        Assert.That(stillHere, Is.True, $"Izložba '{title}' je nestala iako je kliknuto otkazivanje.");
    }
    [Test]
    public async Task Delete_Succeeds_When_No_Dependents()
    {
        var museum = await EnsureMuseumAsync("Niš");
        var title = Unique("DEL Izložba (ok)");
        await CreateExhibitionUIAsync(title, museum);

        await OpenDeleteForAsync(title);
        await ClickAnyAsync("Obriši", "Delete", "Confirm");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe"));

        var present = await RowVisibleInIndexAsync(title, retries: 3, delayMs: 500);
        Assert.That(present, Is.False, $"Izložba '{title}' i dalje postoji nakon uspešnog brisanja.");
    }
    [Test]
    public async Task Delete_Prevented_When_Order_Depends_On_Exhibition()
    {
        var museum = await EnsureMuseumAsync("Novi Sad");
        var title = Unique("DEL Izložba (blocked)");
        var ticket = Unique("DEL Ticket");

        await CreateExhibitionUIAsync(title, museum);
        await CreateTicketTypeForAsync(museum, ticket);
        await CreateOrderUsingAsync(ticket, title);

        await OpenDeleteForAsync(title);
        await ClickAnyAsync("Obriši", "Delete", "Confirm");
        try
        {
            var alert = Page.Locator(".alert-danger, .alert-warning, .validation-summary-errors");
            if (await alert.CountAsync() > 0) await Expect(alert.First).ToBeVisibleAsync();
        }
        catch { }

        await OpenExhibitionsIndexAsync();
        var stillHere = await RowVisibleInIndexAsync(title);
        Assert.That(stillHere, Is.True, $"Izložba '{title}' je obrisana iako postoji zavisna porudžbina.");
    }
}
