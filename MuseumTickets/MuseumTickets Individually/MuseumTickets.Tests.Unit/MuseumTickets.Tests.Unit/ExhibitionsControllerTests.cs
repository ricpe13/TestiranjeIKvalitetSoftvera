using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuseumTickets.Api.Controllers;
using MuseumTickets.Api.Data;
using MuseumTickets.Api.Domain;
using MuseumTickets.Tests.Unit.Helpers;
using NUnit.Framework;

namespace MuseumTickets.Tests.Unit;

[TestFixture]
public class ExhibitionsControllerTests
{
    private AppDbContext _db = null!;
    private ExhibitionsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _db = TestDb.NewContext();
        TestDb.SeedMuseums(_db);
        _controller = new ExhibitionsController(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Get_All_Returns_Empty_When_No_Data()
    {
        var result = await _controller.GetExhibitions();
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!, Is.Empty);
    }

    [Test]
    public async Task Get_All_Returns_Items_When_Seeded()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        _db.Exhibitions.AddRange(
            new Exhibition { Title = "Stalna", StartDate = DateTime.Today, MuseumId = museumId },
            new Exhibition { Title = "Gostujuća", StartDate = DateTime.Today.AddDays(7), MuseumId = museumId }
        );
        await _db.SaveChangesAsync();

        var result = await _controller.GetExhibitions();

        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Count, Is.EqualTo(2));
    }
    [Test]
    public async Task Get_All_Returns_Correct_Field_Values()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var x = new Exhibition
        {
            Title = "Specimen",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(3),
            Description = "Opis",
            MuseumId = museumId
        };
        _db.Exhibitions.Add(x);
        await _db.SaveChangesAsync();

        var result = await _controller.GetExhibitions();

        Assert.That(result.Value, Is.Not.Null);
        var item = result.Value!.Single();
        Assert.That(item.Title, Is.EqualTo("Specimen"));
        Assert.That(item.Description, Is.EqualTo("Opis"));
        Assert.That(item.StartDate.Date, Is.EqualTo(DateTime.Today));
        Assert.That(item.EndDate!.Value.Date, Is.EqualTo(DateTime.Today.AddDays(3).Date));
        Assert.That(item.MuseumId, Is.EqualTo(museumId));
    }

    [Test]
    public async Task Get_ById_Returns_Item_When_Exists()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var x = new Exhibition { Title = "Specijal", StartDate = DateTime.Today, MuseumId = museumId };
        _db.Exhibitions.Add(x);
        await _db.SaveChangesAsync();

        var result = await _controller.GetExhibition(x.Id);

        Assert.That(result.Result, Is.Null);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Id, Is.EqualTo(x.Id));
        Assert.That(result.Value!.Title, Is.EqualTo("Specijal"));
    }

    [Test]
    public async Task Get_ById_Returns_NotFound_When_Missing()
    {
        var result = await _controller.GetExhibition(9999);
        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }
    [Test]
    public async Task Get_ById_Returns_NotFound_For_NonPositive_Id()
    {
        var result0 = await _controller.GetExhibition(0);
        var resultNeg = await _controller.GetExhibition(-10);

        Assert.That(result0.Result, Is.InstanceOf<NotFoundResult>());
        Assert.That(resultNeg.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Post_Creates_And_Returns_CreatedAt_With_Id()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var dto = new Exhibition
        {
            Title = "Nova",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(10),
            Description = "Opis",
            MuseumId = museumId
        };

        var result = await _controller.PostExhibition(dto);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);

        var body = created!.Value as Exhibition;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.Id, Is.GreaterThan(0));

        var inDb = await _db.Exhibitions.FindAsync(body!.Id);
        Assert.That(inDb, Is.Not.Null);
        Assert.That(inDb!.Title, Is.EqualTo("Nova"));
    }

    [Test]
    public async Task Post_Returns_BadRequest_When_Museum_Not_Exists()
    {
        var dto = new Exhibition
        {
            Title = "BezMuzeja",
            StartDate = DateTime.Today,
            MuseumId = 999_999
        };

        var result = await _controller.PostExhibition(dto);
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Put_Returns_BadRequest_When_RouteId_Differs_From_Body()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var x = new Exhibition { Title = "A", StartDate = DateTime.Today, MuseumId = museumId };
        _db.Exhibitions.Add(x);
        await _db.SaveChangesAsync();

        var body = new Exhibition { Id = x.Id, Title = "X", StartDate = x.StartDate, MuseumId = museumId };
        var result = await _controller.PutExhibition(x.Id + 1, body);

        Assert.That(result, Is.InstanceOf<BadRequestResult>());
    }

    [Test]
    public async Task Put_Returns_NotFound_When_Entity_Missing()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var body = new Exhibition { Id = 9999, Title = "X", StartDate = DateTime.Today, MuseumId = museumId };
        var result = await _controller.PutExhibition(9999, body);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Put_Updates_Entity_When_Valid()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var x = new Exhibition
        {
            Title = "Staro",
            StartDate = DateTime.Today,
            Description = "Opis",
            MuseumId = museumId
        };
        _db.Exhibitions.Add(x);
        await _db.SaveChangesAsync();
        var body = new Exhibition
        {
            Id = x.Id,
            Title = "Novo",
            StartDate = x.StartDate.Date.AddDays(1),
            EndDate = x.StartDate.Date.AddDays(7),
            Description = "Novi opis",
            MuseumId = museumId
        };
        var result = await _controller.PutExhibition(x.Id, body);
        Assert.That(result, Is.InstanceOf<NoContentResult>());
        var reloaded = await _db.Exhibitions
            .AsNoTracking()
            .FirstAsync(e => e.Id == x.Id);
        Assert.That(reloaded.Title, Is.EqualTo("Novo"));
        Assert.That(reloaded.Description, Is.EqualTo("Novi opis"));
        Assert.That(reloaded.StartDate.Date, Is.EqualTo(body.StartDate.Date));
        Assert.That(reloaded.EndDate!.Value.Date, Is.EqualTo(body.EndDate!.Value.Date));
    }

    [Test]
    public async Task Delete_NoContent_When_No_Dependencies()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var x = new Exhibition { Title = "Samostalna", StartDate = DateTime.Today, MuseumId = museumId };
        _db.Exhibitions.Add(x);
        await _db.SaveChangesAsync();

        var result = await _controller.DeleteExhibition(x.Id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        var still = await _db.Exhibitions.FindAsync(x.Id);
        Assert.That(still, Is.Null);
    }

    [Test]
    public async Task Delete_Returns_Conflict_When_Orders_Exist()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var tt = new TicketType { Name = "Osnovna", Price = 500, MuseumId = museumId };
        var ex = new Exhibition { Title = "Povezana", StartDate = DateTime.Today, MuseumId = museumId };
        _db.TicketTypes.Add(tt);
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();
        _db.Orders.Add(new Order
        {
            BuyerName = "Pera",
            Quantity = 1,
            OrderedAt = DateTime.UtcNow,
            TicketTypeId = tt.Id,
            ExhibitionId = ex.Id
        });
        await _db.SaveChangesAsync();

        var result = await _controller.DeleteExhibition(ex.Id);

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
    }
}
