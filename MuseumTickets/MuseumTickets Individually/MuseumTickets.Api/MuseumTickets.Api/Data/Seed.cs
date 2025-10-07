using MuseumTickets.Api.Domain;

namespace MuseumTickets.Api.Data;

public static class Seed
{
    public static void Run(AppDbContext db)
    {
        if (db.Museums.Any()) return;
        var m1 = new Museum
        {
            Name = "National Museum",
            City = "Belgrade",
            Description = "Largest museum with diverse collections."
        };
        var m2 = new Museum
        {
            Name = "City Museum",
            City = "Niš",
            Description = "Regional exhibits and local history."
        };
        db.Museums.AddRange(m1, m2);
        db.SaveChanges();
        var ex1 = new Exhibition
        {
            Title = "Roman Heritage",
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today.AddDays(60),
            MuseumId = m2.Id,
            Description = "Artifacts from the Roman period."
        };
        var ex2 = new Exhibition
        {
            Title = "Modern Art",
            StartDate = DateTime.Today.AddDays(-10),
            EndDate = null,
            MuseumId = m1.Id,
            Description = "Contemporary installations."
        };
        db.Exhibitions.AddRange(ex1, ex2);
        db.SaveChanges();
        var t1 = new TicketType
        {
            Name = "General Admission",
            Price = 1200m,
            MuseumId = m1.Id,
            Description = "Standard entry"
        };
        var t2 = new TicketType
        {
            Name = "Student",
            Price = 800m,
            MuseumId = m1.Id,
            Description = "Discounted for students"
        };
        var t3 = new TicketType
        {
            Name = "Standard",
            Price = 600m,
            MuseumId = m2.Id
        };
        var t4 = new TicketType
        {
            Name = "VIP",
            Price = 1500m,
            MuseumId = m2.Id,
            Description = "Priority access"
        };
        db.TicketTypes.AddRange(t1, t2, t3, t4);
        db.SaveChanges();
        var o1 = new Order
        {
            BuyerName = "Pavle Perić",
            BuyerEmail = "pavle@example.com",
            Quantity = 2,
            TicketTypeId = t1.Id,
            ExhibitionId = ex2.Id,
            OrderedAt = DateTime.UtcNow.AddDays(-2)
        };
        var o2 = new Order
        {
            BuyerName = "Ana Jovanović",
            BuyerEmail = "ana@example.com",
            Quantity = 1,
            TicketTypeId = t3.Id,
            ExhibitionId = ex1.Id,
            OrderedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.Orders.AddRange(o1, o2);
        db.SaveChanges();
    }
}
