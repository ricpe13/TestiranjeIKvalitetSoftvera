using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuseumTickets.Api.Domain;

public class Order
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string BuyerName { get; set; } = string.Empty;

    [EmailAddress, MaxLength(120)]
    public string? BuyerEmail { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;

    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(TicketType))]
    public int TicketTypeId { get; set; }
    public TicketType? TicketType { get; set; }
    [ForeignKey(nameof(Exhibition))]
    public int ExhibitionId { get; set; }
    public Exhibition? Exhibition { get; set; }
}
