using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MuseumTickets.Api.Controllers;
using MuseumTickets.Api.Data;
using MuseumTickets.Api.Domain;
using MuseumTickets.Tests.Unit.Helpers;
using NUnit.Framework;

namespace MuseumTickets.Tests.Unit;

[TestFixture]
public class MuseumsControllerTests
{
    private AppDbContext _db = null!;
    private MuseumsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _db = TestDb.NewContext();
        _controller = new MuseumsController(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Get_All_Returns_Empty_When_No_Data()
    {
        var result = await _controller.GetMuseums();
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!, Is.Empty);
    }

    [Test]
    public async Task Get_All_Returns_Items_When_Seeded()
    {
        _db.Museums.AddRange(
            new Museum { Name = "Narodni muzej", City = "Beograd", Description = "Nacionalna zbirka" },
            new Museum { Name = "MSU", City = "Beograd", Description = "Savremena umetnost" }
        );
        await _db.SaveChangesAsync();

        var result = await _controller.GetMuseums();

        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Count, Is.EqualTo(2));
    }
    [Test]
    public async Task Get_All_Returns_Correct_Field_Values()
    {
        _db.Museums.Add(new Museum
        {
            Name = "Muzej Nikole Tesle",
            City = "Beograd",
            Description = "Tesla"
        });
        await _db.SaveChangesAsync();

        var result = await _controller.GetMuseums();

        Assert.That(result.Value, Is.Not.Null);
        var item = result.Value!.Single();
        Assert.That(item.Name, Is.EqualTo("Muzej Nikole Tesle"));
        Assert.That(item.City, Is.EqualTo("Beograd"));
        Assert.That(item.Description, Does.Contain("Tesla"));
    }

    [Test]
    public async Task Get_ById_Returns_Item_When_Exists()
    {
        var m = new Museum { Name = "Narodni muzej", City = "Beograd" };
        _db.Museums.Add(m);
        await _db.SaveChangesAsync();

        var result = await _controller.GetMuseum(m.Id);

        Assert.That(result.Result, Is.Null);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Id, Is.EqualTo(m.Id));
        Assert.That(result.Value!.Name, Is.EqualTo("Narodni muzej"));
    }

    [Test]
    public async Task Get_ById_Returns_NotFound_When_Missing()
    {
        var result = await _controller.GetMuseum(9999);
        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }
    [Test]
    public async Task Get_ById_Returns_NotFound_For_NonPositive_Id()
    {
        var result0 = await _controller.GetMuseum(0);
        var resultNeg = await _controller.GetMuseum(-5);

        Assert.That(result0.Result, Is.InstanceOf<NotFoundResult>());
        Assert.That(resultNeg.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Post_Creates_And_Returns_CreatedAt_With_Id()
    {
        var dto = new Museum { Name = "Muzej Vojvodine", City = "Novi Sad", Description = "Opis" };

        var result = await _controller.PostMuseum(dto);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);

        var body = created!.Value as Museum;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.Id, Is.GreaterThan(0));

        var inDb = await _db.Museums.FindAsync(body!.Id);
        Assert.That(inDb, Is.Not.Null);
        Assert.That(inDb!.Name, Is.EqualTo("Muzej Vojvodine"));
    }

    [Test]
    public async Task Put_Returns_BadRequest_When_RouteId_Differs_From_Body()
    {
        var m = new Museum { Name = "A", City = "B" };
        _db.Museums.Add(m);
        await _db.SaveChangesAsync();

        var body = new Museum { Id = m.Id, Name = "X", City = "Y" };
        var result = await _controller.PutMuseum(m.Id + 1, body);

        Assert.That(result, Is.InstanceOf<BadRequestResult>());
    }

    [Test]
    public async Task Put_Returns_NotFound_When_Entity_Missing()
    {
        var body = new Museum { Id = 9999, Name = "X", City = "Y" };
        var result = await _controller.PutMuseum(9999, body);
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Put_Updates_Entity_When_Valid()
    {
        var m = new Museum { Name = "Staro", City = "Stari Grad", Description = "Opis" };
        _db.Museums.Add(m);
        await _db.SaveChangesAsync();

        var body = new Museum { Id = m.Id, Name = "Novo", City = "Beograd", Description = "Novi opis" };
        var result = await _controller.PutMuseum(m.Id, body);

        Assert.That(result, Is.InstanceOf<NoContentResult>());

        var reloaded = await _db.Museums.FindAsync(m.Id);
        Assert.That(reloaded!.Name, Is.EqualTo("Novo"));
        Assert.That(reloaded!.City, Is.EqualTo("Beograd"));
        Assert.That(reloaded!.Description, Is.EqualTo("Novi opis"));
    }

    [Test]
    public async Task Delete_NoContent_When_No_Dependencies()
    {
        var m = new Museum { Name = "Samostalan", City = "Beograd" };
        _db.Museums.Add(m);
        await _db.SaveChangesAsync();

        var result = await _controller.DeleteMuseum(m.Id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        var still = await _db.Museums.FindAsync(m.Id);
        Assert.That(still, Is.Null);
    }

    [Test]
    public async Task Delete_Returns_Conflict_When_Orders_Exist_Via_TicketTypes_Or_Exhibitions()
    {
        var m = new Museum { Name = "Vezuje", City = "Bg" };
        _db.Museums.Add(m);
        await _db.SaveChangesAsync();
        var tt = new TicketType { Name = "Osnovna", Price = 500, MuseumId = m.Id };
        var ex = new Exhibition { Title = "Stalna", StartDate = DateTime.Today, MuseumId = m.Id };
        _db.TicketTypes.Add(tt);
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();
        var ord = new Order
        {
            BuyerName = "Pera",
            Quantity = 1,
            OrderedAt = DateTime.UtcNow,
            TicketTypeId = tt.Id,
            ExhibitionId = ex.Id
        };
        _db.Orders.Add(ord);
        await _db.SaveChangesAsync();

        var result = await _controller.DeleteMuseum(m.Id);

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
    }
}
