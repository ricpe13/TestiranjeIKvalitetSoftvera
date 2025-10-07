namespace MuseumTickets.Web.Models;

public class TicketTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public int MuseumId { get; set; }
}
