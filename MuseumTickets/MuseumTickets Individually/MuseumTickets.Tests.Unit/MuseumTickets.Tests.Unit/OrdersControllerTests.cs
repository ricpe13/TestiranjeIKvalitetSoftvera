using System;
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
public class OrdersControllerTests
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
    public async Task Get_All_Returns_Empty_When_No_Data()
    {
        var result = await _controller.GetOrders();
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!, Is.Empty);
    }

    [Test]
    public async Task Get_All_Returns_Items_When_Seeded()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var tt = new TicketType { Name = "Osnovna", Price = 500, MuseumId = museumId };
        var ex = new Exhibition { Title = "Stalna", StartDate = DateTime.Today, MuseumId = museumId };
        _db.TicketTypes.Add(tt);
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();

        TestDb.SeedOneOrder(_db, tt.Id, ex.Id);
        TestDb.SeedOneOrder(_db, tt.Id, ex.Id);

        var result = await _controller.GetOrders();

        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Count, Is.EqualTo(2));
    }
    [Test]
    public async Task Get_All_Returns_Correct_Field_Values()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var tt = new TicketType { Name = "Family", Price = 1500, MuseumId = museumId };
        var ex = new Exhibition { Title = "Specimen", StartDate = DateTime.Today, MuseumId = museumId };
        _db.TicketTypes.Add(tt);
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();

        var when = new DateTime(2024, 1, 2, 15, 30, 0, DateTimeKind.Utc);
        var o = new Order
        {
            BuyerName = "Ana",
            BuyerEmail = "ana@example.com",
            Quantity = 2,
            TicketTypeId = tt.Id,
            ExhibitionId = ex.Id,
            OrderedAt = when
        };
        _db.Orders.Add(o);
        await _db.SaveChangesAsync();

        var result = await _controller.GetOrders();

        Assert.That(result.Value, Is.Not.Null);
        var item = result.Value!.Single();
        Assert.That(item.BuyerName, Is.EqualTo("Ana"));
        Assert.That(item.BuyerEmail, Is.EqualTo("ana@example.com"));
        Assert.That(item.Quantity, Is.EqualTo(2));
        Assert.That(item.TicketTypeId, Is.EqualTo(tt.Id));
        Assert.That(item.ExhibitionId, Is.EqualTo(ex.Id));
        Assert.That(item.OrderedAt, Is.EqualTo(when));
    }

    [Test]
    public async Task Get_ById_Returns_Item_When_Exists()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var tt = new TicketType { Name = "Osnovna", Price = 500, MuseumId = museumId };
        var ex = new Exhibition { Title = "Specijal", StartDate = DateTime.Today, MuseumId = museumId };
        _db.TicketTypes.Add(tt);
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();

        var o = TestDb.SeedOneOrder(_db, tt.Id, ex.Id);

        var result = await _controller.GetOrder(o.Id);

        Assert.That(result.Result, Is.Null);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Id, Is.EqualTo(o.Id));
        Assert.That(result.Value!.TicketTypeId, Is.EqualTo(tt.Id));
        Assert.That(result.Value!.ExhibitionId, Is.EqualTo(ex.Id));
    }

    [Test]
    public async Task Get_ById_Returns_NotFound_When_Missing()
    {
        var result = await _controller.GetOrder(9999);
        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }
    [Test]
    public async Task Get_ById_Returns_NotFound_For_NonPositive_Id()
    {
        var result0 = await _controller.GetOrder(0);
        var resultNeg = await _controller.GetOrder(-7);

        Assert.That(result0.Result, Is.InstanceOf<NotFoundResult>());
        Assert.That(resultNeg.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Post_Creates_And_Returns_CreatedAt_With_Id()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var tt = new TicketType { Name = "Family", Price = 1200, MuseumId = museumId };
        var ex = new Exhibition { Title = "Nova", StartDate = DateTime.Today, MuseumId = museumId };
        _db.TicketTypes.Add(tt);
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();

        var dto = new Order
        {
            BuyerName = "Petar",
            BuyerEmail = "petar@example.com",
            Quantity = 3,
            TicketTypeId = tt.Id,
            ExhibitionId = ex.Id,
            OrderedAt = DateTime.UtcNow
        };

        var result = await _controller.PostOrder(dto);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);

        var body = created!.Value as Order;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.Id, Is.GreaterThan(0));

        var inDb = await _db.Orders.FindAsync(body!.Id);
        Assert.That(inDb, Is.Not.Null);
        Assert.That(inDb!.BuyerName, Is.EqualTo("Petar"));
        Assert.That(inDb!.TicketTypeId, Is.EqualTo(tt.Id));
        Assert.That(inDb!.ExhibitionId, Is.EqualTo(ex.Id));
    }

    [Test]
    public async Task Post_Returns_BadRequest_When_TicketType_Not_Exists()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var ex = new Exhibition { Title = "X", StartDate = DateTime.Today, MuseumId = museumId };
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();

        var dto = new Order
        {
            BuyerName = "Neko",
            Quantity = 1,
            TicketTypeId = 999999,
            ExhibitionId = ex.Id
        };

        var result = await _controller.PostOrder(dto);
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Post_Returns_BadRequest_When_Exhibition_Not_Exists()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var tt = new TicketType { Name = "Osnovna", Price = 500, MuseumId = museumId };
        _db.TicketTypes.Add(tt);
        await _db.SaveChangesAsync();

        var dto = new Order
        {
            BuyerName = "Neko",
            Quantity = 1,
            TicketTypeId = tt.Id,
            ExhibitionId = 999999
        };

        var result = await _controller.PostOrder(dto);
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Post_Returns_BadRequest_When_TT_And_Exhibition_Belong_To_Different_Museums()
    {
        var mA = _db.Museums.First();
        var mB = new Museum { Name = "Drugi", City = "NS" };
        _db.Museums.Add(mB);
        await _db.SaveChangesAsync();

        var tt = new TicketType { Name = "A", Price = 100, MuseumId = mA.Id };
        var ex = new Exhibition { Title = "B", StartDate = DateTime.Today, MuseumId = mB.Id };
        _db.TicketTypes.Add(tt);
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();

        var dto = new Order
        {
            BuyerName = "Mix",
            Quantity = 1,
            TicketTypeId = tt.Id,
            ExhibitionId = ex.Id
        };

        var result = await _controller.PostOrder(dto);
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Put_Returns_BadRequest_When_RouteId_Differs_From_Body()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var tt = new TicketType { Name = "T1", Price = 100, MuseumId = museumId };
        var ex = new Exhibition { Title = "E1", StartDate = DateTime.Today, MuseumId = museumId };
        _db.TicketTypes.Add(tt);
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();

        var o = TestDb.SeedOneOrder(_db, tt.Id, ex.Id);
        var body = new Order { Id = o.Id, BuyerName = "X", Quantity = 5, TicketTypeId = tt.Id, ExhibitionId = ex.Id, OrderedAt = o.OrderedAt };

        var result = await _controller.PutOrder(o.Id + 1, body);
        Assert.That(result, Is.InstanceOf<BadRequestResult>());
    }

    [Test]
    public async Task Put_Returns_NotFound_When_Entity_Missing()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var tt = new TicketType { Name = "T1", Price = 100, MuseumId = museumId };
        var ex = new Exhibition { Title = "E1", StartDate = DateTime.Today, MuseumId = museumId };
        _db.TicketTypes.Add(tt);
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();

        var body = new Order { Id = 9999, BuyerName = "X", Quantity = 5, TicketTypeId = tt.Id, ExhibitionId = ex.Id, OrderedAt = DateTime.UtcNow };

        var result = await _controller.PutOrder(9999, body);
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Put_Returns_BadRequest_When_Invalid_FK_Or_Mismatched_Museum()
    {
        var mA = _db.Museums.First();
        var mB = new Museum { Name = "Drugi", City = "NS" };
        _db.Museums.Add(mB);
        await _db.SaveChangesAsync();

        var ttA = new TicketType { Name = "A", Price = 100, MuseumId = mA.Id };
        var exB = new Exhibition { Title = "B", StartDate = DateTime.Today, MuseumId = mB.Id };
        _db.TicketTypes.Add(ttA);
        _db.Exhibitions.Add(exB);
        await _db.SaveChangesAsync();
        var exA = new Exhibition { Title = "Aexp", StartDate = DateTime.Today, MuseumId = mA.Id };
        _db.Exhibitions.Add(exA);
        await _db.SaveChangesAsync();

        var order = TestDb.SeedOneOrder(_db, ttA.Id, exA.Id);
        var body = new Order
        {
            Id = order.Id,
            BuyerName = "Pera",
            Quantity = 5,
            TicketTypeId = ttA.Id,
            ExhibitionId = exB.Id,
            OrderedAt = order.OrderedAt
        };

        var result = await _controller.PutOrder(order.Id, body);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Put_Updates_Entity_When_Valid_And_Preserves_OrderedAt()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();

        var tt1 = new TicketType { Name = "T1", Price = 100, MuseumId = museumId };
        var tt2 = new TicketType { Name = "T2", Price = 150, MuseumId = museumId };
        var ex1 = new Exhibition { Title = "E1", StartDate = DateTime.Today, MuseumId = museumId };
        var ex2 = new Exhibition { Title = "E2", StartDate = DateTime.Today.AddDays(1), MuseumId = museumId };
        _db.TicketTypes.AddRange(tt1, tt2);
        _db.Exhibitions.AddRange(ex1, ex2);
        await _db.SaveChangesAsync();

        var order = TestDb.SeedOneOrder(_db, tt1.Id, ex1.Id);
        var originalOrderedAt = order.OrderedAt;

        var body = new Order
        {
            Id = order.Id,
            BuyerName = "Novi Kupac",
            BuyerEmail = "novi@example.com",
            Quantity = 4,
            TicketTypeId = tt2.Id,
            ExhibitionId = ex2.Id,
            OrderedAt = originalOrderedAt.AddDays(10)
        };

        var result = await _controller.PutOrder(order.Id, body);
        Assert.That(result, Is.InstanceOf<NoContentResult>());

        var reloaded = await _db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        Assert.That(reloaded.BuyerName, Is.EqualTo("Novi Kupac"));
        Assert.That(reloaded.BuyerEmail, Is.EqualTo("novi@example.com"));
        Assert.That(reloaded.Quantity, Is.EqualTo(4));
        Assert.That(reloaded.TicketTypeId, Is.EqualTo(tt2.Id));
        Assert.That(reloaded.ExhibitionId, Is.EqualTo(ex2.Id));
        Assert.That(reloaded.OrderedAt, Is.EqualTo(originalOrderedAt));
    }

    [Test]
    public async Task Delete_NotFound_When_Missing()
    {
        var result = await _controller.DeleteOrder(123456);
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_NoContent_When_Exists()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var tt = new TicketType { Name = "Osnovna", Price = 500, MuseumId = museumId };
        var ex = new Exhibition { Title = "Stalna", StartDate = DateTime.Today, MuseumId = museumId };
        _db.TicketTypes.Add(tt);
        _db.Exhibitions.Add(ex);
        await _db.SaveChangesAsync();

        var o = TestDb.SeedOneOrder(_db, tt.Id, ex.Id);

        var result = await _controller.DeleteOrder(o.Id);
        Assert.That(result, Is.InstanceOf<NoContentResult>());

        var still = await _db.Orders.FindAsync(o.Id);
        Assert.That(still, Is.Null);
    }
}
