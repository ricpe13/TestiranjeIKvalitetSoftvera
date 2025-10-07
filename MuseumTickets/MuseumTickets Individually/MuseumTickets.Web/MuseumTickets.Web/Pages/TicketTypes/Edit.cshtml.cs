using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using MuseumTickets.Web.Models;

namespace MuseumTickets.Web.Pages.TicketTypes;

public class EditModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EditModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<SelectListItem> MuseumOptions { get; private set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int Id { get; set; }

        [Display(Name = "Naziv")]
        [Required(ErrorMessage = "Polje {0} je obavezno.")]
        [StringLength(80, ErrorMessage = "{0} može imati najviše {1} karaktera.")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Cena")]
        [Range(0, 100000, ErrorMessage = "{0} mora biti između {1} i {2}.")]
        public decimal Price { get; set; }

        [Display(Name = "Opis")]
        [StringLength(500, ErrorMessage = "{0} može imati najviše {1} karaktera.")]
        public string? Description { get; set; }


        [Display(Name = "Muzej")]
        [Required(ErrorMessage = "Polje {0} je obavezno.")]
        public int MuseumId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = _httpClientFactory.CreateClient("Api");

        var museums = await client.GetFromJsonAsync<List<MuseumDto>>("api/Museums") ?? new List<MuseumDto>();
        MuseumOptions = museums
            .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name })
            .ToList();

        var item = await client.GetFromJsonAsync<TicketTypeDto>($"api/TicketTypes/{id}");
        if (item == null) return NotFound();

        Input = new InputModel
        {
            Id = item.Id,
            Name = item.Name,
            Price = item.Price,
            Description = item.Description,
            MuseumId = item.MuseumId
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

        var dto = new TicketTypeDto
        {
            Id = Input.Id,
            Name = Input.Name,
            Price = Input.Price,
            Description = Input.Description,
            MuseumId = Input.MuseumId
        };

        var client = _httpClientFactory.CreateClient("Api");
        var resp = await client.PutAsJsonAsync($"api/TicketTypes/{id}", dto);

        if (resp.IsSuccessStatusCode)
            return RedirectToPage("Index");

        var errorText = await resp.Content.ReadAsStringAsync();
        ModelState.AddModelError(string.Empty, $"Greška API-ja: {resp.StatusCode} – {errorText}");

        await OnGetAsync(id);
        return Page();
    }
}
