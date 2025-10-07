using Microsoft.EntityFrameworkCore;
using MuseumTickets.Api.Data;
using MuseumTickets.Api.Domain;
using System.Net.Sockets;
using System;

namespace MuseumTickets.Tests.Unit.Helpers;

public static class TestDb
{
    public static AppDbContext NewContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
        .Options;

        var ctx = new AppDbContext(options);
        return ctx;
    }

    public static void SeedMuseums(AppDbContext db)
    {
        if (!db.Museums.Any())
        {
            db.Museums.AddRange(
                new Museum { Name = "Narodni muzej", City = "Beograd", Description = "Nacionalna zbirka" },
                new Museum { Name = "Muzej savremene umetnosti", City = "Beograd", Description = "MSU Beograd" }
            );
            db.SaveChanges();
        }
    }

    public static TicketType SeedOneTicketType(AppDbContext db, int? museumId = null)
    {
        var mid = museumId ?? db.Museums.Select(m => m.Id).First();
        var t = new TicketType
        {
            Name = "Osnovna",
            Price = 600,
            Description = "Regular",
            MuseumId = mid
        };
        db.TicketTypes.Add(t);
        db.SaveChanges();
        return t;
    }

    public static Exhibition SeedOneExhibition(AppDbContext db, int? museumId = null)
    {
        var mid = museumId ?? db.Museums.Select(m => m.Id).First();
        var e = new Exhibition
        {
            Title = "Stalna postavka",
            StartDate = DateTime.Today,
            Description = "Opis",
            MuseumId = mid
        };
        db.Exhibitions.Add(e);
        db.SaveChanges();
        return e;
    }

    public static Order SeedOneOrder(AppDbContext db, int ticketTypeId, int exhibitionId)
    {
        var o = new Order
        {
            BuyerName = "Petar Petrović",
            BuyerEmail = "petar@example.com",
            Quantity = 2,
            OrderedAt = DateTime.UtcNow,
            TicketTypeId = ticketTypeId,
            ExhibitionId = exhibitionId
        };
        db.Orders.Add(o);
        db.SaveChanges();
        return o;
    }
}
