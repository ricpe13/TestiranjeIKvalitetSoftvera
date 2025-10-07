
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MuseumTickets.Tests.E2E;

[NonParallelizable]
[Category("AppendOnlyE2E")]
public class MuseumsUpdateE2ETests : PageTest
{
    private string BaseUrl => (Environment.GetEnvironmentVariable("E2E_BASEURL") ?? "http://localhost:7036").TrimEnd('/');
    private static string Unique(string prefix) => $"{prefix} {DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}".Substring(0, 40);
    private async Task OpenIndexAsync()
    {
        await Page.GotoAsync($"{BaseUrl}/Muzeji");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji.*", RegexOptions.IgnoreCase));
    }

    private async Task ClickAnyAsync(params string[] labels)
    {
        foreach (var l in labels)
        {
            var btn = Page.GetByRole(AriaRole.Button, new() { Name = l });
            if (await btn.CountAsync() > 0 && await btn.First.IsVisibleAsync())
            {
                await btn.First.ClickAsync();
                return;
            }
        }
        foreach (var l in labels)
        {
            var link = Page.GetByRole(AriaRole.Link, new() { Name = l });
            if (await link.CountAsync() > 0 && await link.First.IsVisibleAsync())
            {
                await link.First.ClickAsync();
                return;
            }
        }
        foreach (var l in labels)
        {
            var txt = Page.GetByText(l, new() { Exact = false });
            if (await txt.CountAsync() > 0 && await txt.First.IsVisibleAsync())
            {
                await txt.First.ClickAsync();
                return;
            }
        }
        Assert.Fail($"Nije pronađeno dugme/link/tekst: {string.Join(", ", labels)}");
    }

    private async Task FillByLabelAsync(string label, string value, bool required = true)
    {
        var el = Page.GetByLabel(label);
        if (await el.CountAsync() > 0)
        {
            await el.First.FillAsync(value);
        }
        else if (required)
        {
            Assert.Fail($"Polje sa etiketom '{label}' nije pronađeno.");
        }
    }

    private async Task CreateMuseumUIAsync(string name, string city, string? desc = null)
    {
        await OpenIndexAsync();
        await ClickAnyAsync("+ Kreiraj", "Kreiraj", "Create New", "Dodaj", "Add");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji/Kreiraj"));

        await FillByLabelAsync("Naziv", name);
        await FillByLabelAsync("Grad", city);
        if (!string.IsNullOrWhiteSpace(desc))
            await FillByLabelAsync("Opis", desc, required: false);

        await ClickAnyAsync("Sačuvaj", "Snimi", "Kreiraj", "Save", "Create");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji"));
    }

    private async Task OpenEditForAsync(string museumName)
    {
        await OpenIndexAsync();
        var row = Page.GetByRole(AriaRole.Row, new() { Name = museumName });
        await Expect(row).ToBeVisibleAsync();

        var editLink = row.GetByRole(AriaRole.Link, new() { Name = "Izmeni" });
        if (await editLink.CountAsync() == 0)
        {
            editLink = row.GetByRole(AriaRole.Button, new() { Name = "Izmeni" });
        }
        await editLink.First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji/Izmeni/\\d+"));
    }

    private async Task<bool> RowContainsAsync(string museumName, string text, int retries = 4, int delayMs = 500)
    {
        for (int i = 0; i < retries; i++)
        {
            var row = Page.GetByRole(AriaRole.Row, new() { Name = museumName });
            if (await row.CountAsync() > 0 && await row.First.IsVisibleAsync())
            {
                var cell = row.GetByRole(AriaRole.Cell, new() { Name = text });
                if (await cell.CountAsync() > 0 && await cell.First.IsVisibleAsync())
                    return true;

                var anyText = row.GetByText(text, new() { Exact = false });
                if (await anyText.CountAsync() > 0 && await anyText.First.IsVisibleAsync())
                    return true;
            }
            await Page.ReloadAsync();
            await Page.WaitForTimeoutAsync(delayMs);
        }
        return false;
    }
    [Test]
    public async Task Edit_ChangeCity_Saves()
    {
        var name = Unique("UPD Muzej");
        await CreateMuseumUIAsync(name, "Zaječar", "pre update");

        await OpenEditForAsync(name);
        await FillByLabelAsync("Grad", "Beograd");
        await ClickAnyAsync("Sačuvaj izmene", "Sačuvaj", "Snimi", "Save", "Update");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji"));

        var ok = await RowContainsAsync(name, "Beograd");
        Assert.That(ok, Is.True, $"Nakon izmene, red za '{name}' ne prikazuje grad 'Beograd'.");
    }
    [Test]
    public async Task Edit_Cancel_DoesNotSave()
    {
        var name = Unique("UPD Muzej (cancel)");
        await CreateMuseumUIAsync(name, "Čačak");

        await OpenEditForAsync(name);
        await FillByLabelAsync("Grad", "Subotica");
        await ClickAnyAsync("Otkaži", "Odustani", "Nazad", "Cancel", "Back");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji"));

        var stillOld = await RowContainsAsync(name, "Čačak");
        Assert.That(stillOld, Is.True, $"Grad za '{name}' je promenjen iako je kliknuto otkazivanje.");
    }
    [Test]
    public async Task Edit_LongDescription_Saves_And_ShowsInDetails()
    {
        var name = Unique("UPD Muzej (opis)");
        await CreateMuseumUIAsync(name, "Kraljevo");

        await OpenEditForAsync(name);
        var longDesc = new string('x', 320);
        await FillByLabelAsync("Opis", longDesc, required: false);
        await ClickAnyAsync("Sačuvaj izmene", "Sačuvaj", "Snimi", "Save", "Update");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji"));

        var row = Page.GetByRole(AriaRole.Row, new() { Name = name });
        await Expect(row).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Link, new() { Name = "Detalji" }).ClickAsync();

        await Expect(Page.Locator("table")).ToBeVisibleAsync();
        await Expect(Page.GetByText(longDesc, new() { Exact = false })).ToBeVisibleAsync();
    }
}
