using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using MuseumTickets.Web.Models;

namespace MuseumTickets.Web.Pages.Museums;

public class EditModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EditModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int Id { get; set; }

        [Display(Name = "Naziv")]
        [Required(ErrorMessage = "Polje {0} je obavezno.")]
        [StringLength(120, ErrorMessage = "{0} može imati najviše {1} karaktera.")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Grad")]
        [Required(ErrorMessage = "Polje {0} je obavezno.")]
        [StringLength(80, ErrorMessage = "{0} može imati najviše {1} karaktera.")]
        public string City { get; set; } = string.Empty;

        [Display(Name = "Opis")]
        [StringLength(2000, ErrorMessage = "{0} može imati najviše {1} karaktera.")]
        public string? Description { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = _httpClientFactory.CreateClient("Api");
        var item = await client.GetFromJsonAsync<MuseumDto>($"api/Museums/{id}");
        if (item == null) return NotFound();

        Input = new InputModel
        {
            Id = item.Id,
            Name = item.Name,
            City = item.City,
            Description = item.Description
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid) return Page();
        if (id != Input.Id)
        {
            ModelState.AddModelError(string.Empty, "Neusklađen ID u ruti i formi.");
            return Page();
        }

        var dto = new MuseumDto
        {
            Id = Input.Id,
            Name = Input.Name,
            City = Input.City,
            Description = Input.Description
        };

        var client = _httpClientFactory.CreateClient("Api");
        var resp = await client.PutAsJsonAsync($"api/Museums/{id}", dto);

        if (resp.IsSuccessStatusCode)
            return RedirectToPage("Index");

        var errorText = await resp.Content.ReadAsStringAsync();
        ModelState.AddModelError(string.Empty, $"Greška API-ja: {resp.StatusCode} – {errorText}");
        return Page();
    }
}
