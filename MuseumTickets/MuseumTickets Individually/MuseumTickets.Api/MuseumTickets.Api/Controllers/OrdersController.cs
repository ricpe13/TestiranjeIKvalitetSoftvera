using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuseumTickets.Api.Data;
using MuseumTickets.Api.Domain;

namespace MuseumTickets.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        => await _context.Orders.AsNoTracking().ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var item = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (item == null) return NotFound();
        return item;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> PostOrder(Order order)
    {
        if (string.IsNullOrWhiteSpace(order.BuyerName))
            return BadRequest("Ime kupca je obavezno.");
        if (order.Quantity <= 0)
            return BadRequest("Količina mora biti veća od 0.");

        var ticketType = await _context.TicketTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == order.TicketTypeId);
        if (ticketType == null) return BadRequest("Ne postoji tip karte sa zadatim TicketTypeId.");

        var exhibition = await _context.Exhibitions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == order.ExhibitionId);
        if (exhibition == null) return BadRequest("Ne postoji izložba sa zadatim ExhibitionId.");

        if (ticketType.MuseumId != exhibition.MuseumId)
            return BadRequest("Tip karte i izložba moraju pripadati istom muzeju.");

        order.Id = 0;
        if (order.OrderedAt == default) order.OrderedAt = DateTime.UtcNow;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutOrder(int id, Order order)
    {
        if (id != order.Id) return BadRequest();
        if (string.IsNullOrWhiteSpace(order.BuyerName))
            return BadRequest("Ime kupca je obavezno.");
        if (order.Quantity <= 0)
            return BadRequest("Količina mora biti veća od 0.");

        var existing = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (existing == null) return NotFound();

        var ticketType = await _context.TicketTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == order.TicketTypeId);
        if (ticketType == null) return BadRequest("Ne postoji tip karte sa zadatim TicketTypeId.");

        var exhibition = await _context.Exhibitions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == order.ExhibitionId);
        if (exhibition == null) return BadRequest("Ne postoji izložba sa zadatim ExhibitionId.");

        if (ticketType.MuseumId != exhibition.MuseumId)
            return BadRequest("Tip karte i izložba moraju pripadati istom muzeju.");

        existing.BuyerName = order.BuyerName;
        existing.BuyerEmail = order.BuyerEmail;
        existing.Quantity = order.Quantity;
        existing.TicketTypeId = order.TicketTypeId;
        existing.ExhibitionId = order.ExhibitionId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var entity = await _context.Orders.FindAsync(id);
        if (entity == null) return NotFound();

        _context.Orders.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
