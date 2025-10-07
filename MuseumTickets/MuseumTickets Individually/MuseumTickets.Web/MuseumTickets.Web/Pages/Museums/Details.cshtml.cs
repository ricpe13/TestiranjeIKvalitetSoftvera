using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using MuseumTickets.Web.Models;

namespace MuseumTickets.Web.Pages.Museums;

public class DetailsModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DetailsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public MuseumDto? Item { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            Item = await client.GetFromJsonAsync<MuseumDto>($"api/Museums/{id}");
            if (Item == null) return NotFound();
            return Page();
        }
        catch (Exception ex)
        {
            Error = $"Greška prilikom učitavanja: {ex.Message}";
            return Page();
        }
    }
}
