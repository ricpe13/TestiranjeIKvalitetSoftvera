using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuseumTickets.Api.Domain;

public class TicketType
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100000)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
    [ForeignKey(nameof(Museum))]
    public int MuseumId { get; set; }
    public Museum? Museum { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
