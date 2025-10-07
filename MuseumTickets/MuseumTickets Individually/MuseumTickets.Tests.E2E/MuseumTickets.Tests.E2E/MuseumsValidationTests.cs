using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MuseumTickets.Tests.E2E;

[NonParallelizable]
public class MuseumsValidationTests : PageTest
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

    private async Task<int> GetRowCount() =>
        await Page.Locator("table tbody tr").CountAsync();

    private async Task WaitRowCountIncreases(int initialCount, int timeoutMs = 25000)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            var now = await GetRowCount();
            if (now >= initialCount + 1) return;
            await Page.ReloadAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        Assert.Fail($"Broj redova u tabeli se nije povećao sa {initialCount} na bar {initialCount + 1} u roku od {timeoutMs} ms.");
    }

    private async Task<bool> AnyValidationShown()
    {
        var summary = Page.Locator(".validation-summary-errors,.text-danger,.field-validation-error,[data-valmsg-summary='true']");
        if (await summary.CountAsync() > 0) return true;
        var invalids = Page.Locator("[aria-invalid='true']");
        return await invalids.CountAsync() > 0;
    }
    [Test]
    public async Task Required_Shown_On_Empty_Submit()
    {
        await Page.GotoAsync(BaseUrl);
        await Nav("/Muzeji").ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "+ Kreiraj" }).First.ClickAsync();

        await ClickSubmit();
        Assert.That(await AnyValidationShown(), Is.True, "Očekivana validaciona poruka se ne vidi.");
        var cancel = Page.GetByRole(AriaRole.Link, new() { Name = "Otkaži" });
        if (await cancel.CountAsync() > 0) await cancel.First.ClickAsync();
        await EnsureOnList("/Muzeji");
    }
    [Test]
    public async Task Create_Succeeds_And_Appears_In_List()
    {
        var mName = $"E2E Val Muzej {Sfx}";

        await Page.GotoAsync(BaseUrl);
        await Nav("/Muzeji").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var beforeCount = await GetRowCount();

        await Page.GetByRole(AriaRole.Link, new() { Name = "+ Kreiraj" }).First.ClickAsync();
        await FillSmart(mName, "Naziv", "Input_Name");
        await FillSmart("Beograd", "Grad", "Input_City");
        await ClickSubmit();

        await EnsureOnList("/Muzeji");
        await WaitRowCountIncreases(beforeCount);
        var ourRow = Page.Locator("table tbody tr", new() { HasTextString = mName }).First;
        if (await ourRow.CountAsync() > 0)
        {
            await ourRow.GetByRole(AriaRole.Link, new() { Name = "Obriši" }).First.ClickAsync();
            await ClickSubmit();
            await EnsureOnList("/Muzeji");
        }
    }
}
