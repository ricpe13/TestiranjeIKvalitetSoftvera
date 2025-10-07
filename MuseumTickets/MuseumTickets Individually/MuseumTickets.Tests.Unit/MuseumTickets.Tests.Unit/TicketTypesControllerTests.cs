using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MuseumTickets.Api.Controllers;
using MuseumTickets.Api.Data;
using MuseumTickets.Api.Domain;
using MuseumTickets.Tests.Unit.Helpers;
using NUnit.Framework;

namespace MuseumTickets.Tests.Unit;

[TestFixture]
public class TicketTypesControllerTests
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
    public async Task Get_All_Returns_Empty_When_No_Data()
    {
        var result = await _controller.GetTicketTypes();
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!, Is.Empty);
    }

    [Test]
    public async Task Get_All_Returns_Items_When_Seeded()
    {
        TestDb.SeedOneTicketType(_db);
        TestDb.SeedOneTicketType(_db);

        var result = await _controller.GetTicketTypes();

        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Count, Is.EqualTo(2));
    }
    [Test]
    public async Task Get_All_Returns_Correct_Field_Values()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var t = new TicketType { Name = "Student", Price = 300, MuseumId = museumId };
        _db.TicketTypes.Add(t);
        await _db.SaveChangesAsync();

        var result = await _controller.GetTicketTypes();

        Assert.That(result.Value, Is.Not.Null);
        var item = result.Value!.Single();
        Assert.That(item.Name, Is.EqualTo("Student"));
        Assert.That(item.Price, Is.EqualTo(300));
        Assert.That(item.MuseumId, Is.EqualTo(museumId));
    }

    [Test]
    public async Task Get_ById_Returns_Item_When_Exists()
    {
        var t = TestDb.SeedOneTicketType(_db);
        var result = await _controller.GetTicketType(t.Id);

        Assert.That(result.Result, Is.Null);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Id, Is.EqualTo(t.Id));
    }

    [Test]
    public async Task Get_ById_Returns_NotFound_When_Missing()
    {
        var result = await _controller.GetTicketType(9999);
        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }
    [Test]
    public async Task Get_ById_Returns_NotFound_For_NonPositive_Id()
    {
        var result0 = await _controller.GetTicketType(0);
        var resultNeg = await _controller.GetTicketType(-3);

        Assert.That(result0.Result, Is.InstanceOf<NotFoundResult>());
        Assert.That(resultNeg.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Post_Creates_And_Returns_CreatedAt_With_Id()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var dto = new TicketType { Name = "Family", Price = 1200, MuseumId = museumId };

        var result = await _controller.PostTicketType(dto);

        var created = result.Result as CreatedAtActionResult;
        var body = created!.Value as TicketType;

        Assert.That(body!.Id, Is.GreaterThan(0));
        var inDb = await _db.TicketTypes.FindAsync(body!.Id);
        Assert.That(inDb, Is.Not.Null);
    }

    [Test]
    public async Task Post_Returns_BadRequest_When_Museum_Not_Exists()
    {
        var dto = new TicketType { Name = "Invalid", Price = 500, MuseumId = 999_999 };
        var result = await _controller.PostTicketType(dto);
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Put_Returns_BadRequest_When_RouteId_Differs_From_Body()
    {
        var t = TestDb.SeedOneTicketType(_db);
        var result = await _controller.PutTicketType(t.Id + 1, new TicketType
        {
            Id = t.Id,
            Name = "X",
            Price = 1,
            MuseumId = t.MuseumId
        });
        Assert.That(result, Is.InstanceOf<BadRequestResult>());
    }

    [Test]
    public async Task Put_Returns_NotFound_When_Entity_Missing()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var body = new TicketType { Id = 9999, Name = "Novi", Price = 700, MuseumId = museumId };

        var result = await _controller.PutTicketType(9999, body);
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Put_Updates_Entity_When_Valid()
    {
        var t = TestDb.SeedOneTicketType(_db);
        var body = new TicketType { Id = t.Id, Name = "Izmenjena", Price = 999, MuseumId = t.MuseumId };

        var result = await _controller.PutTicketType(t.Id, body);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        var reloaded = await _db.TicketTypes.FindAsync(t.Id);
        Assert.That(reloaded!.Name, Is.EqualTo("Izmenjena"));
        Assert.That(reloaded!.Price, Is.EqualTo(999));
    }

    [Test]
    public async Task Delete_Returns_NotFound_When_Missing()
    {
        var result = await _controller.DeleteTicketType(123456);
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_Returns_Conflict_When_Orders_Exist()
    {
        var t = TestDb.SeedOneTicketType(_db);
        var ex = TestDb.SeedOneExhibition(_db, t.MuseumId);
        TestDb.SeedOneOrder(_db, t.Id, ex.Id);

        var result = await _controller.DeleteTicketType(t.Id);
        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
    }
}
