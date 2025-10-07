using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuseumTickets.Api.Domain;

public class Exhibition
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }
    [ForeignKey(nameof(Museum))]
    public int MuseumId { get; set; }
    public Museum? Museum { get; set; }
}
