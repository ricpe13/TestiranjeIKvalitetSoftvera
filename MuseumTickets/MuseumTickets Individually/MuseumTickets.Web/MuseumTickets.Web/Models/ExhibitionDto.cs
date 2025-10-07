namespace MuseumTickets.Web.Models;

public class ExhibitionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
    public int MuseumId { get; set; }
}
