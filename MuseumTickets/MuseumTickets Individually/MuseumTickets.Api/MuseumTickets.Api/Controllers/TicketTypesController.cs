using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuseumTickets.Api.Data;
using MuseumTickets.Api.Domain;

namespace MuseumTickets.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketTypesController : ControllerBase
{
    private readonly AppDbContext _db;

    public TicketTypesController(AppDbContext db)
    {
        _db = db;
    }
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketTypeDto>>> GetTicketTypes()
    {
        var list = await (
            from t in _db.TicketTypes.AsNoTracking()
            join m in _db.Museums.AsNoTracking() on t.MuseumId equals m.Id into mj
            from m in mj.DefaultIfEmpty()
            select new TicketTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                Price = t.Price,
                Description = t.Description,
                MuseumId = t.MuseumId,
                MuseumName = m != null ? m.Name : "(nepoznat)"
            }
        ).ToListAsync();

        return list;
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketTypeDto>> GetTicketType(int id)
    {
        var dto = await (
            from t in _db.TicketTypes.AsNoTracking().Where(x => x.Id == id)
            join m in _db.Museums.AsNoTracking() on t.MuseumId equals m.Id into mj
            from m in mj.DefaultIfEmpty()
            select new TicketTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                Price = t.Price,
                Description = t.Description,
                MuseumId = t.MuseumId,
                MuseumName = m != null ? m.Name : "(nepoznat)"
            }
        ).FirstOrDefaultAsync();

        if (dto == null) return NotFound();
        return dto;
    }
    [HttpPost]
    public async Task<ActionResult<TicketType>> PostTicketType([FromBody] TicketType input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            ModelState.AddModelError(nameof(input.Name), "Naziv je obavezan.");
        if (input.Price < 0)
            ModelState.AddModelError(nameof(input.Price), "Cena ne može biti negativna.");

        var museumExists = await _db.Museums.AnyAsync(m => m.Id == input.MuseumId);
        if (!museumExists)
            ModelState.AddModelError(nameof(input.MuseumId), "Izabrani muzej ne postoji.");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var entity = new TicketType
        {
            Name = input.Name.Trim(),
            Price = input.Price,
            Description = input.Description,
            MuseumId = input.MuseumId,
        };

        _db.TicketTypes.Add(entity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTicketType), new { id = entity.Id }, entity);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutTicketType(int id, [FromBody] TicketType input)
    {
        if (id != input.Id) return BadRequest();

        if (string.IsNullOrWhiteSpace(input.Name))
            ModelState.AddModelError(nameof(input.Name), "Naziv je obavezan.");
        if (input.Price < 0)
            ModelState.AddModelError(nameof(input.Price), "Cena ne može biti negativna.");

        var museumExists = await _db.Museums.AnyAsync(m => m.Id == input.MuseumId);
        if (!museumExists)
            ModelState.AddModelError(nameof(input.MuseumId), "Izabrani muzej ne postoji.");

        if (!ModelState.IsValid) return BadRequest(ModelState);

        var entity = await _db.TicketTypes.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Name = input.Name.Trim();
        entity.Price = input.Price;
        entity.Description = input.Description;

        await _db.SaveChangesAsync();
        return NoContent();
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTicketType(int id)
    {
        var entity = await _db.TicketTypes.FindAsync(id);
        if (entity == null) return NotFound();

        var hasOrders = await _db.Orders.AnyAsync(o => o.TicketTypeId == id);
        if (hasOrders)
            return Conflict("Postoje porudžbine koje koriste ovaj tip karte.");

        _db.TicketTypes.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
public sealed class TicketTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public int MuseumId { get; set; }
    public string MuseumName { get; set; } = default!;
}
