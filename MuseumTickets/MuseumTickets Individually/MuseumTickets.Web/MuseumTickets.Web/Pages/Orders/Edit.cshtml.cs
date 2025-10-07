using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using MuseumTickets.Web.Models;

namespace MuseumTickets.Web.Pages.Orders;

public class EditModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EditModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<SelectListItem> TicketTypeOptions { get; private set; } = new();
    public List<SelectListItem> ExhibitionOptions { get; private set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int Id { get; set; }

        [Display(Name = "Ime i prezime kupca")]
        [Required(ErrorMessage = "Polje {0} je obavezno.")]
        [StringLength(120, ErrorMessage = "{0} može imati najviše {1} karaktera.")]
        public string BuyerName { get; set; } = string.Empty;

        [Display(Name = "Email kupca")]
        [EmailAddress(ErrorMessage = "Unesite ispravan email.")]
        [StringLength(120)]
        public string? BuyerEmail { get; set; }

        [Display(Name = "Količina")]
        [Range(1, 1000, ErrorMessage = "{0} mora biti između {1} i {2}.")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Tip karte")]
        [Required(ErrorMessage = "Polje {0} je obavezno.")]
        public int TicketTypeId { get; set; }

        [Display(Name = "Izložba")]
        [Required(ErrorMessage = "Polje {0} je obavezno.")]
        public int ExhibitionId { get; set; }
        public DateTime OrderedAt { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = _httpClientFactory.CreateClient("Api");

        var tickets = await client.GetFromJsonAsync<List<TicketTypeDto>>("api/TicketTypes") ?? new List<TicketTypeDto>();
        TicketTypeOptions = tickets.Select(t => new SelectListItem { Value = t.Id.ToString(), Text = $"{t.Name} (#{t.Id})" }).ToList();

        var exhibitions = await client.GetFromJsonAsync<List<ExhibitionDto>>("api/Exhibitions") ?? new List<ExhibitionDto>();
        ExhibitionOptions = exhibitions.Select(e => new SelectListItem { Value = e.Id.ToString(), Text = $"{e.Title} (#{e.Id})" }).ToList();

        var item = await client.GetFromJsonAsync<OrderDto>($"api/Orders/{id}");
        if (item == null) return NotFound();

        Input = new InputModel
        {
            Id = item.Id,
            BuyerName = item.BuyerName,
            BuyerEmail = item.BuyerEmail,
            Quantity = item.Quantity,
            TicketTypeId = item.TicketTypeId,
            ExhibitionId = item.ExhibitionId,
            OrderedAt = item.OrderedAt
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync(id);
            return Page();
        }

        if (id != Input.Id)
        {
            ModelState.AddModelError(string.Empty, "Neusklađen ID u ruti i formi.");
            await OnGetAsync(id);
            return Page();
        }

        var dto = new OrderDto
        {
            Id = Input.Id,
            BuyerName = Input.BuyerName,
            BuyerEmail = Input.BuyerEmail,
            Quantity = Input.Quantity,
            TicketTypeId = Input.TicketTypeId,
            ExhibitionId = Input.ExhibitionId,
            OrderedAt = Input.OrderedAt
        };

        var client = _httpClientFactory.CreateClient("Api");
        var resp = await client.PutAsJsonAsync($"api/Orders/{id}", dto);

        if (resp.IsSuccessStatusCode)
            return RedirectToPage("Index");

        var errorText = await resp.Content.ReadAsStringAsync();
        ModelState.AddModelError(string.Empty, $"Greška API-ja: {resp.StatusCode} – {errorText}");
        await OnGetAsync(id);
        return Page();
    }
}
