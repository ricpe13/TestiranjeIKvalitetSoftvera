using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuseumTickets.Api.Controllers;
using MuseumTickets.Api.Data;
using MuseumTickets.Api.Domain;
using NUnit.Framework;

namespace MuseumTickets.Tests.Unit;

[TestFixture]
public class MuseumsControllerEdgeTests
{
    private AppDbContext _db = null!;
    private MuseumsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        _controller = new MuseumsController(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();
    [Test]
    public async Task Post_BadRequest_When_Name_Or_City_Empty()
    {
        var dto = new Museum { Name = "   ", City = "" };

        var result = await _controller.PostMuseum(dto);
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }
    [Test]
    public async Task Post_BadRequest_When_Name_TooLong()
    {
        var longName = new string('X', 256);
        var dto = new Museum
        {
            Name = longName,
            City = "Beograd",
            Description = "Opis"
        };

        var result = await _controller.PostMuseum(dto);
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }
    [Test]
    public async Task Put_BadRequest_When_Name_Empty()
    {
        var m = new Museum { Name = "Stari", City = "Bg" };
        _db.Museums.Add(m);
        await _db.SaveChangesAsync();

        var body = new Museum { Id = m.Id, Name = "  ", City = "Bg" };
        var result = await _controller.PutMuseum(m.Id, body);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }
    [Test]
    public async Task Delete_NotFound_When_IdMissing()
    {
        var result = await _controller.DeleteMuseum(999_999);
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}
