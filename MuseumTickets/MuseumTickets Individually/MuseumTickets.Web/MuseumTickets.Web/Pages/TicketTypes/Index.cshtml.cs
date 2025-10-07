using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using MuseumTickets.Web.Models;

namespace MuseumTickets.Web.Pages.TicketTypes;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<TicketTypeDto> Items { get; private set; } = new();
    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            var data = await client.GetFromJsonAsync<List<TicketTypeDto>>("api/TicketTypes");
            Items = data ?? new List<TicketTypeDto>();
        }
        catch (Exception ex)
        {
            Error = $"Nije moguće pristupiti API-ju. {ex.GetType().Name}: {ex.Message}";
            Items = new List<TicketTypeDto>();
        }
    }
}
