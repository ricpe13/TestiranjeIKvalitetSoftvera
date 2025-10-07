using Microsoft.AspNetCore.Mvc;
using MuseumTickets.Api.Controllers;
using MuseumTickets.Api.Data;
using MuseumTickets.Api.Domain;
using MuseumTickets.Tests.Unit.Helpers;
using NUnit.Framework;

namespace MuseumTickets.Tests.Unit;

[TestFixture]
public class TicketTypesControllerEdgeTests
{
    private AppDbContext _db = null!;
    private TicketTypesController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _db = TestDb.NewContext();
        TestDb.SeedMuseums(_db);
        _controller = new TicketTypesController(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Post_BadRequest_When_Price_Negative()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();

        var dto = new TicketType
        {
            Name = "Pogrešno",
            Price = -10,
            MuseumId = museumId
        };

        var result = await _controller.PostTicketType(dto);
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Post_BadRequest_When_Name_Empty()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var dto = new TicketType { Name = "  ", Price = 100, MuseumId = museumId };

        var result = await _controller.PostTicketType(dto);
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Put_BadRequest_When_Price_Negative()
    {
        var t = TestDb.SeedOneTicketType(_db);
        var body = new TicketType { Id = t.Id, Name = "X", Price = -5, MuseumId = t.MuseumId };

        var result = await _controller.PutTicketType(t.Id, body);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }
    [Test]
    public async Task Delete_NoContent_When_No_Dependencies()
    {
        var m = _db.Museums.First();
        var tt = new TicketType
        {
            Name = "Adult",
            Price = 500,
            MuseumId = m.Id,
            Description = "Basic"
        };
        _db.TicketTypes.Add(tt);
        await _db.SaveChangesAsync();

        var result = await _controller.DeleteTicketType(tt.Id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.False(_db.TicketTypes.Any(x => x.Id == tt.Id));
    }
}
