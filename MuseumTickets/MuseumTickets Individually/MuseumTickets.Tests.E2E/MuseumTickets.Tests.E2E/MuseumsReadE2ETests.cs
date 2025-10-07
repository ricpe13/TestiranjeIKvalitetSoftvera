using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MuseumTickets.Tests.E2E;

[NonParallelizable]
[Category("AppendOnlyE2E")]
public class MuseumsReadE2ETests : PageTest
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

    private async Task<bool> RowVisibleInIndexAsync(string museumName, int retries = 8, int delayMs = 700)
    {
        var cell = Page.GetByRole(AriaRole.Cell, new() { Name = museumName });
        for (int i = 0; i < retries; i++)
        {
            if (await cell.CountAsync() > 0 && await cell.First.IsVisibleAsync()) return true;
            await Page.ReloadAsync();
            await Page.WaitForTimeoutAsync(delayMs);
        }
        return false;
    }

    private async Task OpenDetailsFromRowAsync(string museumName)
    {
        var row = Page.GetByRole(AriaRole.Row, new() { Name = museumName });
        await Expect(row).ToBeVisibleAsync();
        await row.GetByRole(AriaRole.Link, new() { Name = "Detalji" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji/Detalji/\\d+"));
    }
    [Test]
    public async Task Index_Shows_Item_After_Reload()
    {
        var name = Unique("READ Muzej");
        await CreateMuseumUIAsync(name, "Beograd", "read-check");

        await OpenIndexAsync();
        await Page.ReloadAsync();

        var found = await RowVisibleInIndexAsync(name);
        Assert.That(found, Is.True, $"Muzej '{name}' nije vidljiv u listi posle reload-a.");
    }
    [Test]
    public async Task Details_Displays_Correct_Fields()
    {
        var name = Unique("READ Muzej (detalji)");
        var city = "Niš";
        var desc = "Opis za provere detalja (READ)";

        await CreateMuseumUIAsync(name, city, desc);

        await OpenIndexAsync();
        await OpenDetailsFromRowAsync(name);

        await Expect(Page.Locator("table")).ToBeVisibleAsync();
        await Expect(Page.GetByText(name, new() { Exact = false })).ToBeVisibleAsync();
        await Expect(Page.GetByText(city, new() { Exact = false })).ToBeVisibleAsync();
        await Expect(Page.GetByText(desc, new() { Exact = false })).ToBeVisibleAsync();
    }
    [Test]
    public async Task Details_Back_Navigates_To_Index_And_Item_Remains()
    {
        var name = Unique("READ Muzej (back)");
        await CreateMuseumUIAsync(name, "Kragujevac");

        await OpenIndexAsync();
        await OpenDetailsFromRowAsync(name);

        await ClickAnyAsync("Nazad", "Back");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Muzeji"));

        var found = await RowVisibleInIndexAsync(name);
        Assert.That(found, Is.True, $"Muzej '{name}' nije vidljiv u listi nakon povratka sa Detalja.");
    }
}
