using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MuseumTickets.Tests.E2E;

[NonParallelizable]
public class OrdersValidationTests : PageTest
{
    private string BaseUrl => Environment.GetEnvironmentVariable("E2E_BASEURL") ?? "http://localhost:7036";
    private string Sfx => DateTime.Now.ToString("yyyyMMddHHmmss");
    private ILocator Nav(string path) => Page.Locator($"a[href='{path}']").First;

    private async Task FillSmart(string text, params string[] labelsOrIds)
    {
        foreach (var l in labelsOrIds)
        {
            var byLabel = Page.GetByLabel(l);
            if (await byLabel.CountAsync() > 0) { await byLabel.First.FillAsync(text); return; }
            var byId = Page.Locator($"#{l}");
            if (await byId.CountAsync() > 0) { await byId.First.FillAsync(text); return; }
        }
        throw new Exception($"Nije nađeno polje za: {string.Join(", ", labelsOrIds)}");
    }

    private async Task ClickSubmit()
    {
        var candidates = new[]
        {
            Page.Locator("form button[type='submit']").First,
            Page.Locator("form input[type='submit']").First,
            Page.GetByRole(AriaRole.Button, new() { Name = "Kreiraj" }).First,
            Page.GetByRole(AriaRole.Button, new() { Name = "Sačuvaj" }).First,
            Page.GetByRole(AriaRole.Button, new() { Name = "Snimi" }).First,
            Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).First,
            Page.GetByRole(AriaRole.Button, new() { Name = "Create" }).First,
        };
        foreach (var c in candidates)
        {
            if (await c.CountAsync() > 0) { await c.ClickAsync(); return; }
        }
        throw new Exception("Nisam našao dugme za snimanje (submit).");
    }

    [Test]
    public async Task Missing_Selects_Are_Validated()
    {
        var museumName = $"E2E ORD Muzej {Sfx}";
        var ticketName = $"E2E ORD Tip {Sfx}";
        var exTitle = $"E2E ORD Ex {Sfx}";
        await Page.GotoAsync(BaseUrl);
        await Nav("/Muzeji").ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Kreiraj" }).First.ClickAsync();
        await FillSmart(museumName, "Naziv", "Input_Name");
        await FillSmart("Subotica", "Grad", "Input_City");
        await ClickSubmit();
        await Nav("/TipoviKarata").ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Kreiraj" }).First.ClickAsync();
        await FillSmart(ticketName, "Naziv", "Input_Name");
        await FillSmart("400", "Cena", "Input_Price");
        await Page.GetByLabel("Muzej").First.SelectOptionAsync(new SelectOptionValue { Label = museumName });
        await ClickSubmit();
        await Nav("/Izlozbe").ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Kreiraj" }).First.ClickAsync();
        await FillSmart(exTitle, "Naslov", "Naziv", "Input_Title");
        await FillSmart(DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"), "Datum početka", "Input_StartDate");
        await Page.GetByLabel("Muzej").First.SelectOptionAsync(new SelectOptionValue { Label = museumName });
        await ClickSubmit();
        await Nav("/Porudzbine").ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Kreiraj" }).First.ClickAsync();
        await FillSmart("Test Kupac", "Ime kupca", "Input_BuyerName");
        await FillSmart("1", "Količina", "Input_Quantity");

        await ClickSubmit();

        var ttSelect = Page.Locator("#Input_TicketTypeId").First;
        var exSelect = Page.Locator("#Input_ExhibitionId").First;

        bool ttInvalid = await ttSelect.GetAttributeAsync("aria-invalid") == "true"
                      || await Page.Locator("[data-valmsg-for='Input.TicketTypeId'],.text-danger,.field-validation-error").CountAsync() > 0;

        bool exInvalid = await exSelect.GetAttributeAsync("aria-invalid") == "true"
                      || await Page.Locator("[data-valmsg-for='Input.ExhibitionId'],.text-danger,.field-validation-error").CountAsync() > 0;

        Assert.That(ttInvalid, Is.True, "Očekivana validacija na Tip karte.");
        Assert.That(exInvalid, Is.True, "Očekivana validacija na Izložba.");
        await Page.GoBackAsync();
        await Nav("/TipoviKarata").ClickAsync();
        var ttRow = Page.Locator("table tr", new() { HasTextString = ticketName }).First;
        await ttRow.GetByRole(AriaRole.Link, new() { Name = "Obriši" }).First.ClickAsync();
        await ClickSubmit();

        await Nav("/Izlozbe").ClickAsync();
        var exRow = Page.Locator("table tr", new() { HasTextString = exTitle }).First;
        await exRow.GetByRole(AriaRole.Link, new() { Name = "Obriši" }).First.ClickAsync();
        await ClickSubmit();

        await Nav("/Muzeji").ClickAsync();
        var mRow = Page.Locator("table tr", new() { HasTextString = museumName }).First;
        await mRow.GetByRole(AriaRole.Link, new() { Name = "Obriši" }).First.ClickAsync();
        await ClickSubmit();
    }
}
