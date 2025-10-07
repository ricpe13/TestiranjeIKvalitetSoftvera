using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MuseumTickets.Tests.E2E;

[NonParallelizable]
public class ExhibitionsValidationTests : PageTest
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

    private ILocator AnyError() => Page.Locator(".validation-summary-errors,.text-danger,.field-validation-error,[data-valmsg-summary='true']");

    [Test]
    public async Task EndDate_Before_StartDate_Is_Validated()
    {
        var museumName = $"E2E EX Muzej {Sfx}";
        var exTitle = $"E2E EX {Sfx}";
        var start = DateTime.Today.AddDays(5).ToString("yyyy-MM-dd");
        var end = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
        await Page.GotoAsync(BaseUrl);
        await Nav("/Muzeji").ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Kreiraj" }).First.ClickAsync();
        await FillSmart(museumName, "Naziv", "Input_Name");
        await FillSmart("Niš", "Grad", "Input_City");
        await ClickSubmit();
        await Nav("/Izlozbe").ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Kreiraj" }).First.ClickAsync();

        await FillSmart(exTitle, "Naslov", "Naziv", "Input_Title");
        await FillSmart(start, "Datum početka", "Input_StartDate");
        var endCtrl = Page.GetByLabel("Datum završetka");
        if (await endCtrl.CountAsync() > 0) await endCtrl.First.FillAsync(end);

        await Page.GetByLabel("Muzej").First.SelectOptionAsync(new SelectOptionValue { Label = museumName });
        await ClickSubmit();

        Assert.That(await AnyError().CountAsync(), Is.GreaterThan(0), "Očekivana greška (završetak pre početka).");
        await Nav("/Muzeji").ClickAsync();
        var mRow = Page.Locator("table tr", new() { HasTextString = museumName }).First;
        await mRow.GetByRole(AriaRole.Link, new() { Name = "Obriši" }).First.ClickAsync();
        await ClickSubmit();
    }
}
