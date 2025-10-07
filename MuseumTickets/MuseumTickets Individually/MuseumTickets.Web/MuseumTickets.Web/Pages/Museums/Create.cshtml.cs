using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using MuseumTickets.Web.Models;

namespace MuseumTickets.Web.Pages.Museums;

public class CreateModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CreateModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
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

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var dto = new MuseumDto
        {
            Name = Input.Name,
            City = Input.City,
            Description = Input.Description
        };

        var client = _httpClientFactory.CreateClient("Api");
        var resp = await client.PostAsJsonAsync("api/Museums", dto);

        if (resp.IsSuccessStatusCode)
            return RedirectToPage("Index");

        var errorText = await resp.Content.ReadAsStringAsync();
        ModelState.AddModelError(string.Empty, $"Greška API-ja: {resp.StatusCode} – {errorText}");
        return Page();
    }
}
