using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuseumTickets.Api.Data;
using MuseumTickets.Api.Domain;

namespace MuseumTickets.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExhibitionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ExhibitionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Exhibition>>> GetExhibitions()
        => await _context.Exhibitions.AsNoTracking().ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<Exhibition>> GetExhibition(int id)
    {
        var item = await _context.Exhibitions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        return item;
    }

    [HttpPost]
    public async Task<ActionResult<Exhibition>> PostExhibition(Exhibition exhibition)
    {
        var museumExists = await _context.Museums.AnyAsync(m => m.Id == exhibition.MuseumId);
        if (!museumExists) return BadRequest("Ne postoji Muzej sa zadatim MuseumId.");

        if (exhibition.EndDate.HasValue && exhibition.EndDate.Value < exhibition.StartDate)
            return BadRequest("Datum završetka ne može biti pre datuma početka.");

        exhibition.Id = 0;
        _context.Exhibitions.Add(exhibition);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetExhibition), new { id = exhibition.Id }, exhibition);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutExhibition(int id, Exhibition exhibition)
    {
        if (id != exhibition.Id) return BadRequest();

        var existing = await _context.Exhibitions.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return NotFound();

        var museumExists = await _context.Museums.AnyAsync(m => m.Id == exhibition.MuseumId);
        if (!museumExists) return BadRequest("Ne postoji Muzej sa zadatim MuseumId.");

        if (exhibition.EndDate.HasValue && exhibition.EndDate.Value < exhibition.StartDate)
            return BadRequest("Datum završetka ne može biti pre datuma početka.");

        existing.Title = exhibition.Title;
        existing.StartDate = exhibition.StartDate;
        existing.EndDate = exhibition.EndDate;
        existing.Description = exhibition.Description;
        existing.MuseumId = exhibition.MuseumId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExhibition(int id)
    {
        var entity = await _context.Exhibitions.FindAsync(id);
        if (entity == null) return NotFound();

        var hasOrders = await _context.Orders.AnyAsync(o => o.ExhibitionId == id);
        if (hasOrders)
            return Conflict("Ne može brisanje: postoje porudžbine povezane sa ovom izložbom.");

        _context.Exhibitions.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
