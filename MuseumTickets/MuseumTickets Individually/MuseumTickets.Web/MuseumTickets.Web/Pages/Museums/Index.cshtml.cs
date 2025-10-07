using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using MuseumTickets.Web.Models;

namespace MuseumTickets.Web.Pages.Museums;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<MuseumDto> Items { get; private set; } = new();
    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            var data = await client.GetFromJsonAsync<List<MuseumDto>>("api/Museums");
            Items = data ?? new List<MuseumDto>();
        }
        catch (Exception ex)
        {
            Error = $"Nije moguće pristupiti API-ju. {ex.GetType().Name}: {ex.Message}";
            Items = new List<MuseumDto>();
        }
    }
}
