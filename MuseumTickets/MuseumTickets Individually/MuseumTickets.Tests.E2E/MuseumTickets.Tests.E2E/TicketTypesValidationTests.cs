using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MuseumTickets.Tests.E2E;

[NonParallelizable]
public class TicketTypesValidationTests : PageTest
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
            Page.GetByRole(AriaRole.Button, new() { Name = "Sačuvaj" }).First,
            Page.GetByRole(AriaRole.Button, new() { Name = "Kreiraj" }).First,
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

    private async Task EnsureOnList(string path)
    {
        if (!Page.Url.StartsWith($"{BaseUrl}{path}", StringComparison.OrdinalIgnoreCase))
        {
            var link = Page.Locator($"a[href='{path}']").First;
            if (await link.CountAsync() > 0) await link.ClickAsync();
            else await Page.GotoAsync($"{BaseUrl}{path}");
        }
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task<int> GetRowCount(string tableSelector = "table") =>
        await Page.Locator($"{tableSelector} tbody tr").CountAsync();

    private async Task WaitRowCountIncreases(int initialCount, string tableSelector = "table", int timeoutMs = 25000)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            var now = await GetRowCount(tableSelector);
            if (now >= initialCount + 1) return;
            await Page.ReloadAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        Assert.Fail($"Broj redova u tabeli se nije povećao na listi '{tableSelector}' u zadatom roku.");
    }

    private ILocator AnyError() =>
        Page.Locator(".validation-summary-errors,.text-danger,.field-validation-error,[data-valmsg-summary='true']");
    [Test]
    public async Task Empty_Name_And_Negative_Price_Are_Validated()
    {
        var museumName = $"E2E TT Muzej {Sfx}";
        await Page.GotoAsync(BaseUrl);
        await Nav("/Muzeji").ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "+ Kreiraj" }).First.ClickAsync();
        await FillSmart(museumName, "Naziv", "Input_Name");
        await FillSmart("Novi Sad", "Grad", "Input_City");
        await ClickSubmit();
        await EnsureOnList("/Muzeji");
        await Nav("/TipoviKarata").ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Kreiraj" }).First.ClickAsync();
        await FillSmart("", "Naziv", "Input_Name");
        await FillSmart("100", "Cena", "Input_Price");
        await Page.GetByLabel("Muzej").First.SelectOptionAsync(new SelectOptionValue { Label = museumName });
        await ClickSubmit();
        Assert.That(await AnyError().CountAsync(), Is.GreaterThan(0), "Očekivana greška za Naziv.");
        await FillSmart("E2E Temp", "Naziv", "Input_Name");
        await FillSmart("-10", "Cena", "Input_Price");
        await ClickSubmit();
        Assert.That(await AnyError().CountAsync(), Is.GreaterThan(0), "Očekivana greška za negativnu cenu.");
        var cancel = Page.GetByRole(AriaRole.Link, new() { Name = "Otkaži" });
        if (await cancel.CountAsync() > 0) await cancel.First.ClickAsync();
        await EnsureOnList("/TipoviKarata");
        await Nav("/Muzeji").ClickAsync();
        var mRow = Page.Locator("table tbody tr", new() { HasTextString = museumName }).First;
        if (await mRow.CountAsync() > 0)
        {
            await mRow.GetByRole(AriaRole.Link, new() { Name = "Obriši" }).First.ClickAsync();
            await ClickSubmit();
            await EnsureOnList("/Muzeji");
        }
    }
    [Test]
    public async Task Create_TicketType_Succeeds_And_Shows_In_List()
    {
        var museumName = $"E2E TT Muzej {Sfx}";
        var ttName = $"E2E TT {Sfx}";
        await Page.GotoAsync(BaseUrl);
        await Nav("/Muzeji").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.GetByRole(AriaRole.Link, new() { Name = "+ Kreiraj" }).First.ClickAsync();
        await FillSmart(museumName, "Naziv", "Input_Name");
        await FillSmart("Beograd", "Grad", "Input_City");
        await ClickSubmit();
        await EnsureOnList("/Muzeji");
        await Nav("/TipoviKarata").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var beforeCount = await GetRowCount();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Kreiraj" }).First.ClickAsync();
        await FillSmart(ttName, "Naziv", "Input_Name");
        await FillSmart("500", "Cena", "Input_Price");
        await Page.GetByLabel("Muzej").First.SelectOptionAsync(new SelectOptionValue { Label = museumName });
        await ClickSubmit();

        await EnsureOnList("/TipoviKarata");
        await WaitRowCountIncreases(beforeCount);
        var ttRow = Page.Locator("table tbody tr", new() { HasTextString = ttName }).First;
        if (await ttRow.CountAsync() > 0)
        {
            await ttRow.GetByRole(AriaRole.Link, new() { Name = "Obriši" }).First.ClickAsync();
            await ClickSubmit();
            await EnsureOnList("/TipoviKarata");
        }
        await Nav("/Muzeji").ClickAsync();
        var mRow = Page.Locator("table tbody tr", new() { HasTextString = museumName }).First;
        if (await mRow.CountAsync() > 0)
        {
            await mRow.GetByRole(AriaRole.Link, new() { Name = "Obriši" }).First.ClickAsync();
            await ClickSubmit();
            await EnsureOnList("/Muzeji");
        }
    }
}
