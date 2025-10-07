using Microsoft.AspNetCore.Mvc;
using MuseumTickets.Api.Controllers;
using MuseumTickets.Api.Data;
using MuseumTickets.Api.Domain;
using MuseumTickets.Tests.Unit.Helpers;
using NUnit.Framework;

namespace MuseumTickets.Tests.Unit;

[TestFixture]
public class ExhibitionsControllerEdgeTests
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
    public async Task Post_BadRequest_When_EndDate_Before_StartDate()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();

        var dto = new Exhibition
        {
            Title = "Nevalidna",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(-1),
            MuseumId = museumId
        };

        var result = await _controller.PostExhibition(dto);
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Put_BadRequest_When_EndDate_Before_StartDate()
    {
        var museumId = _db.Museums.Select(m => m.Id).First();
        var e = new Exhibition { Title = "A", StartDate = DateTime.Today, MuseumId = museumId };
        _db.Exhibitions.Add(e);
        await _db.SaveChangesAsync();

        var body = new Exhibition
        {
            Id = e.Id,
            Title = "A",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(-3),
            MuseumId = museumId
        };

        var result = await _controller.PutExhibition(e.Id, body);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }
    [Test]
    public async Task Delete_NotFound_When_IdMissing()
    {
        var result = await _controller.DeleteExhibition(999_999);
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}
