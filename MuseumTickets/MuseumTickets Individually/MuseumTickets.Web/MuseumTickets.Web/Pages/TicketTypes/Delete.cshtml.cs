using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using MuseumTickets.Web.Models;

namespace MuseumTickets.Web.Pages.TicketTypes;

public class DeleteModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DeleteModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public TicketTypeDto? Item { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = _httpClientFactory.CreateClient("Api");
        Item = await client.GetFromJsonAsync<TicketTypeDto>($"api/TicketTypes/{id}");
        if (Item == null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var client = _httpClientFactory.CreateClient("Api");
        var resp = await client.DeleteAsync($"api/TicketTypes/{id}");

        if (resp.IsSuccessStatusCode)
            return RedirectToPage("Index");

        var text = await resp.Content.ReadAsStringAsync();
        Error = $"Greška pri brisanju: {resp.StatusCode} – {text}";
        await OnGetAsync(id);
        return Page();
    }
}
