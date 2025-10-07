namespace MuseumTickets.Web.Models;

public class OrderDto
{
    public int Id { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerEmail { get; set; }
    public int Quantity { get; set; }
    public DateTime OrderedAt { get; set; }
    public int TicketTypeId { get; set; }
    public int ExhibitionId { get; set; }
}
