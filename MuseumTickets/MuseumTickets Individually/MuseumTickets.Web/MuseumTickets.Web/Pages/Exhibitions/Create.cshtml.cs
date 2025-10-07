using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using MuseumTickets.Web.Models;

namespace MuseumTickets.Web.Pages.Exhibitions;

public class CreateModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CreateModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<SelectListItem> MuseumOptions { get; private set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Display(Name = "Naziv izložbe")]
        [Required(ErrorMessage = "Polje {0} je obavezno.")]
        [StringLength(160, ErrorMessage = "{0} može imati najviše {1} karaktera.")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Datum početka")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Display(Name = "Datum završetka (opciono)")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Opis")]
        [StringLength(2000, ErrorMessage = "{0} može imati najviše {1} karaktera.")]
        public string? Description { get; set; }

        [Display(Name = "Muzej")]
        [Required(ErrorMessage = "Polje {0} je obavezno.")]
        public int MuseumId { get; set; }
    }

    public async Task OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient("Api");
        var museums = await client.GetFromJsonAsync<List<MuseumDto>>("api/Museums") ?? new List<MuseumDto>();
        MuseumOptions = museums.Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name }).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var dto = new ExhibitionDto
        {
            Title = Input.Title,
            StartDate = Input.StartDate,
            EndDate = Input.EndDate,
            Description = Input.Description,
            MuseumId = Input.MuseumId
        };

        var client = _httpClientFactory.CreateClient("Api");
        var resp = await client.PostAsJsonAsync("api/Exhibitions", dto);

        if (resp.IsSuccessStatusCode)
            return RedirectToPage("Index");

        var errorText = await resp.Content.ReadAsStringAsync();
        ModelState.AddModelError(string.Empty, $"Greška API-ja: {resp.StatusCode} – {errorText}");
        await OnGetAsync();
        return Page();
    }
}
