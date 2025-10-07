using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuseumTickets.Api.Data;
using MuseumTickets.Api.Domain;

namespace MuseumTickets.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MuseumsController : ControllerBase
{
    private readonly AppDbContext _context;

    public MuseumsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Museum>>> GetMuseums()
        => await _context.Museums.AsNoTracking().ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<Museum>> GetMuseum(int id)
    {
        var m = await _context.Museums.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();
        return m;
    }

    [HttpPost]
    public async Task<ActionResult<Museum>> PostMuseum(Museum museum)
    {
        if (string.IsNullOrWhiteSpace(museum.Name) || string.IsNullOrWhiteSpace(museum.City))
            return BadRequest("Naziv i grad su obavezni.");
        if (museum.Name?.Length > 120)
        {
            ModelState.AddModelError(nameof(museum.Name), "Naziv ne sme biti duži od 120 karaktera.");
            return BadRequest(ModelState);
        }

        museum.Id = 0;
        _context.Museums.Add(museum);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMuseum), new { id = museum.Id }, museum);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutMuseum(int id, Museum museum)
    {
        if (id != museum.Id) return BadRequest();
        if (string.IsNullOrWhiteSpace(museum.Name) || string.IsNullOrWhiteSpace(museum.City))
            return BadRequest("Naziv i grad su obavezni.");
        if (museum.Name?.Length > 120)
        {
            ModelState.AddModelError(nameof(museum.Name), "Naziv ne sme biti duži od 120 karaktera.");
            return BadRequest(ModelState);
        }

        var existing = await _context.Museums.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = museum.Name;
        existing.City = museum.City;
        existing.Description = museum.Description;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMuseum(int id)
    {
        var entity = await _context.Museums.FindAsync(id);
        if (entity == null) return NotFound();

        var hasOrdersViaExhibitions =
            await _context.Orders.AnyAsync(o =>
                _context.Exhibitions.Any(e => e.Id == o.ExhibitionId && e.MuseumId == id));

        var hasOrdersViaTicketTypes =
            await _context.Orders.AnyAsync(o =>
                _context.TicketTypes.Any(t => t.Id == o.TicketTypeId && t.MuseumId == id));

        if (hasOrdersViaExhibitions || hasOrdersViaTicketTypes)
            return Conflict("Ne može brisanje: postoje porudžbine povezane sa ovim muzejem.");

        _context.Museums.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
