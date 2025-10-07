using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MuseumTickets.Api.Domain;

public class Museum
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string City { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public ICollection<Exhibition> Exhibitions { get; set; } = new List<Exhibition>();
}
