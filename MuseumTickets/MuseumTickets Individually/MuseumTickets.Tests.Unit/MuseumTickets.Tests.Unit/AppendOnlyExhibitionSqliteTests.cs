using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

using MuseumTickets.Api.Data;
using MuseumTickets.Api.Domain;

namespace MuseumTickets.Tests.AppendOnly
{
    [TestFixture]
    [Category("AppendOnly")]
    [Parallelizable(ParallelScope.None)]
    public class AppendOnlyExhibitionSqliteTests
    {
        private string _dbPath = null!;

        private static string Unique(string prefix)
            => $"{prefix} {DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6]}";

        private static Random NewRandom() => new Random(Guid.NewGuid().GetHashCode());

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var fromEnv = Environment.GetEnvironmentVariable("TEST_DB_PATH");
            _dbPath = !string.IsNullOrWhiteSpace(fromEnv)
                ? Path.GetFullPath(fromEnv)
                : Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    @"..\..\..\..\..\MuseumTickets.Api\MuseumTickets.Api\App_Data\museum.db"
                ));

            TestContext.WriteLine($"[AppendOnly] SQLite DB: {_dbPath}");
        }

        private AppDbContext CreateContext()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={_dbPath};Cache=Shared")
                .Options;

            var ctx = new AppDbContext(opts);
            ctx.Database.EnsureCreated();
            return ctx;
        }
        private int GetOrCreateRandomMuseumId()
        {
            using var ctx = CreateContext();
            var ids = ctx.Museums.Select(m => m.Id).ToList();
            if (ids.Count == 0)
            {
                var museum = new Museum
                {
                    Name = Unique("AppendOnly Museum"),
                    City = "Test City",
                    Description = "Auto-created for Exhibition append-only test"
                };
                ctx.Museums.Add(museum);
                ctx.SaveChanges();
                return museum.Id;
            }

            var rand = NewRandom();
            return ids[rand.Next(ids.Count)];
        }

        private string CreateUniqueExhibitionTitle(AppDbContext ctx, string basePrefix = "AppendOnly Exhibition")
        {
            string candidate;
            do
            {
                candidate = Unique(basePrefix);
            } while (ctx.Exhibitions.Any(e => e.Title == candidate));
            return candidate;
        }

        [Test]
        public void Always_Adds_New_Exhibition_With_Unique_Title_And_Random_EndDate()
        {
            var museumId = GetOrCreateRandomMuseumId();
            string uniqueTitle;
            using (var ctx = CreateContext())
            {
                uniqueTitle = CreateUniqueExhibitionTitle(ctx);
            }
            var start = DateTime.UtcNow.Date;
            var rand = NewRandom();
            var end = start.AddDays(rand.Next(1, 31));
            using (var writeCtx = CreateContext())
            {
                var entity = new Exhibition
                {
                    Title = uniqueTitle,
                    Description = "Append-only test record",
                    StartDate = start,
                    EndDate = end,
                    MuseumId = museumId
                };

                writeCtx.Exhibitions.Add(entity);
                writeCtx.SaveChanges();

                Assert.That(entity.Id, Is.GreaterThan(0), "Očekivan generisan Id nakon upisa.");
            }
            using (var readCtx = CreateContext())
            {
                var exists = readCtx.Exhibitions.Any(e =>
                    e.Title == uniqueTitle &&
                    e.MuseumId == museumId &&
                    e.StartDate == start &&
                    e.EndDate == end
                );

                Assert.That(exists, Is.True, "Izložba sa očekivanim datumima bi trebalo da postoji u SQLite bazi nakon testa.");
            }
        }
    }
}
