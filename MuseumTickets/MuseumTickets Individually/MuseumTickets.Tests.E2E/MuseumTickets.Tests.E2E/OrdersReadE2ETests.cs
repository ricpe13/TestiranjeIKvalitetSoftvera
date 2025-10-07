
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
public class OrdersReadE2ETests : PageTest
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

    private async Task OpenOrdersIndexAsync()
    {
        await Page.GotoAsync($"{BaseUrl}/Porudzbine");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task<bool> RowVisibleInIndexAsync(string customerName, int retries = 8, int delayMs = 700)
    {
        var row = Page.Locator("tbody tr").Filter(new() { HasTextString = customerName }).First;
        for (int i = 0; i < retries; i++)
        {
            if (await row.CountAsync() > 0 && await row.IsVisibleAsync()) return true;
            await Page.ReloadAsync();
            await Page.WaitForTimeoutAsync(delayMs);
        }
        return false;
    }

    private async Task OpenDetailsFromRowAsync(string customerName)
    {
        await OpenOrdersIndexAsync();
        var ok = await RowVisibleInIndexAsync(customerName);
        Assert.That(ok, Is.True, $"Na listi Porudžbina nije pronađen red sa imenom '{customerName}'.");

        var row = Page.Locator("tbody tr").Filter(new() { HasTextString = customerName }).First;
        var link = row.GetByRole(AriaRole.Link, new() { Name = "Detalji" });
        if (await link.CountAsync() == 0) link = row.GetByText("Detalji", new() { Exact = false });
        await link.First.ClickAsync();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Porudzbine/Detalji/\\d+$"));
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
        var museumName = Unique("E2E Muzej za porudžbinu");
        await Page.GotoAsync($"{BaseUrl}/Muzeji");
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji/Kreiraj$"));
        await FillByLabelAsync("Naziv", museumName);
        await FillByLabelAsync("Grad", city);
        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return museumName;
    }

    private async Task<string> CreateExhibitionForAsync(string museumName)
    {
        var title = Unique("E2E Izložba za porudžbinu");
        var (start, end) = DefaultDates();

        await Page.GotoAsync($"{BaseUrl}/Izlozbe");
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Izlozbe/Kreiraj$"));

        await FillByLabelAsync("Naziv", title);
        await FillByLabelAsync("Datum početka", start);
        await FillByLabelAsync("Datum završetka", end);
        await FillByLabelAsync("Opis", "E2E – exhibition for order", required: false);
        await SelectOptionByContainsAsync(Page.GetByLabel("Muzej"), museumName);

        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return title;
    }

    private async Task<string> CreateTicketTypeForAsync(string museumName, string price = "200")
    {
        var name = Unique("E2E Tip karte za porudžbinu");

        await Page.GotoAsync($"{BaseUrl}/TipoviKarata");
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/TipoviKarata/Kreiraj$"));

        await FillByLabelAsync("Naziv", name);
        await FillByLabelAsync("Cena", price);
        await FillByLabelAsync("Opis", "E2E – ticket for order", required: false);
        await SelectOptionByContainsAsync(Page.GetByLabel("Muzej"), museumName);

        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return name;
    }

    private async Task<(string museum, string exhibition, string ticket)> EnsurePrereqsAsync()
    {
        var museum = await EnsureMuseumAsync();
        var exhibition = await CreateExhibitionForAsync(museum);
        var ticket = await CreateTicketTypeForAsync(museum);
        return (museum, exhibition, ticket);
    }

    private async Task CreateOrderUIAsync(string customerName, string email, string quantity, string ticketName, string exhibitionTitle)
    {
        await OpenOrdersIndexAsync();
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Porudzbine/Kreiraj$"));

        await FillByLabelAsync("Ime kupca", customerName);
        await FillByLabelAsync("Email", email);
        await FillByLabelAsync("Količina", quantity);
        await SelectOptionByContainsAsync(Page.GetByLabel("Tip karte"), ticketName);
        await SelectOptionByContainsAsync(Page.GetByLabel("Izložba"), exhibitionTitle);

        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task AssertDetailByLabelContainsAsync(string label, string expectedSubstring)
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
    public async Task Index_Shows_Item_After_Reload()
    {
        var (museum, exhibition, ticket) = await EnsurePrereqsAsync();
        var customer = Unique("READ Kupac");
        var email = $"read+{DateTime.UtcNow.Ticks}@example.com";

        await CreateOrderUIAsync(customer, email, "2", ticket, exhibition);

        await OpenOrdersIndexAsync();
        await Page.ReloadAsync();

        var found = await RowVisibleInIndexAsync(customer);
        Assert.That(found, Is.True, $"Porudžbina za '{customer}' nije vidljiva u listi posle reload-a.");
    }
    [Test]
    public async Task Details_Displays_Core_Fields()
    {
        var (museum, exhibition, ticket) = await EnsurePrereqsAsync();
        var customer = Unique("READ Kupac (detalji)");
        var email = $"detalji+{DateTime.UtcNow.Ticks}@example.com";

        await CreateOrderUIAsync(customer, email, "3", ticket, exhibition);

        await OpenDetailsFromRowAsync(customer);
        await Expect(Page.Locator("table, dl")).ToBeVisibleAsync();

        await Expect(Page.GetByText(customer, new() { Exact = false })).ToBeVisibleAsync();
        await AssertDetailByLabelContainsAsync("Email", email);
        await AssertDetailByLabelContainsAsync("Količina", "3");
        await AssertDetailByLabelContainsAsync("Tip karte", ticket);
        await AssertDetailByLabelContainsAsync("Izložba", exhibition);
    }
    [Test]
    public async Task Details_Back_Navigates_To_Index_And_Item_Remains()
    {
        var (museum, exhibition, ticket) = await EnsurePrereqsAsync();
        var customer = Unique("READ Kupac (back)");
        var email = $"back+{DateTime.UtcNow.Ticks}@example.com";

        await CreateOrderUIAsync(customer, email, "1", ticket, exhibition);

        await OpenDetailsFromRowAsync(customer);
        await ClickAnyAsync("Nazad", "Back");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await OpenOrdersIndexAsync();
        var found = await RowVisibleInIndexAsync(customer);
        Assert.That(found, Is.True, $"Porudžbina za '{customer}' nije vidljiva u listi nakon povratka sa Detalja.");
    }
}
