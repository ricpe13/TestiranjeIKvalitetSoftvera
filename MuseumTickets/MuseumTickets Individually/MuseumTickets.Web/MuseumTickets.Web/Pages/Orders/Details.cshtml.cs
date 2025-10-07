using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using MuseumTickets.Web.Models;

namespace MuseumTickets.Web.Pages.Orders;

public class DetailsModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DetailsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public OrderDto? Item { get; private set; }
    public string? TicketTypeName { get; private set; }
    public string? ExhibitionTitle { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");

            Item = await client.GetFromJsonAsync<OrderDto>($"api/Orders/{id}");
            if (Item == null) return NotFound();

            var tt = await client.GetFromJsonAsync<TicketTypeDto>($"api/TicketTypes/{Item.TicketTypeId}");
            TicketTypeName = tt?.Name ?? $"#{Item.TicketTypeId}";

            var ex = await client.GetFromJsonAsync<ExhibitionDto>($"api/Exhibitions/{Item.ExhibitionId}");
            ExhibitionTitle = ex?.Title ?? $"#{Item.ExhibitionId}";

            return Page();
        }
        catch (Exception ex)
        {
            Error = $"Greška prilikom učitavanja: {ex.Message}";
            return Page();
        }
    }
}
