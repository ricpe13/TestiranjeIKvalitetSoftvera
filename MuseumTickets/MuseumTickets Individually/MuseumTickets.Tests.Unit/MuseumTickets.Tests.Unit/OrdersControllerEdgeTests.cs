using Microsoft.AspNetCore.Mvc;
using MuseumTickets.Api.Controllers;
using MuseumTickets.Api.Data;
using MuseumTickets.Api.Domain;
using MuseumTickets.Tests.Unit.Helpers;
using NUnit.Framework;

namespace MuseumTickets.Tests.Unit;

[TestFixture]
public class OrdersControllerEdgeTests
{
    private AppDbContext _db = null!;
    private OrdersController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _db = TestDb.NewContext();
        TestDb.SeedMuseums(_db);
        _controller = new OrdersController(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Post_BadRequest_When_Quantity_NonPositive()
    {
        var m = _db.Museums.First();
        var tt = new TicketType { Name = "Osnovna", Price = 500, MuseumId = m.Id };
        var ex = new Exhibition { Title = "Stalna", StartDate = DateTime.Today, MuseumId = m.Id };
        _db.TicketTypes.Add(tt);
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();

        var o = new Order { BuyerName = "Pera", Quantity = 0, TicketTypeId = tt.Id, ExhibitionId = ex.Id };

        var result = await _controller.PostOrder(o);
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Put_BadRequest_When_Quantity_NonPositive()
    {
        var m = _db.Museums.First();
        var tt = new TicketType { Name = "Osnovna", Price = 500, MuseumId = m.Id };
        var ex = new Exhibition { Title = "Stalna", StartDate = DateTime.Today, MuseumId = m.Id };
        _db.TicketTypes.Add(tt);
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();

        var existing = TestDb.SeedOneOrder(_db, tt.Id, ex.Id);

        var body = new Order { Id = existing.Id, BuyerName = "Pera", Quantity = -3, TicketTypeId = tt.Id, ExhibitionId = ex.Id, OrderedAt = existing.OrderedAt };

        var result = await _controller.PutOrder(existing.Id, body);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }
    
    [Test]
    public async Task Post_BadRequest_When_BuyerName_Empty()
    {
        var m = _db.Museums.First();
        var tt = new TicketType { Name = "Osnovna", Price = 500, MuseumId = m.Id };
        var ex = new Exhibition { Title = "Stalna", StartDate = DateTime.Today, MuseumId = m.Id };
        _db.TicketTypes.Add(tt);
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();

        var o = new Order { BuyerName = "   ", Quantity = 1, TicketTypeId = tt.Id, ExhibitionId = ex.Id };

        var result = await _controller.PostOrder(o);
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }
    [Test]
    public async Task Delete_NotFound_When_IdMissing()
    {
        var result = await _controller.DeleteOrder(888_888);
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}
