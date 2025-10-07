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
    public class AppendOnlyTicketTypeSqliteTests
    {
        private string _dbPath = null!;

        private static string Unique(string prefix)
            => $"{prefix} {DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6]}";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var fromEnv = Environment.GetEnvironmentVariable("TEST_DB_PATH");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                _dbPath = Path.GetFullPath(fromEnv);
            }
            else
            {
                _dbPath = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    @"..\..\..\..\..\MuseumTickets.Api\MuseumTickets.Api\App_Data\museum.db"
                ));
            }

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
        private int GetOrCreateMuseumId()
        {
            using var ctx = CreateContext();
            var ids = ctx.Museums.Select(m => m.Id).ToList();

            if (ids.Count == 0)
            {
                var museum = new Museum
                {
                    Name = Unique("AppendOnly Museum"),
                    City = "Test City",
                    Description = "Auto-created for TicketType append-only test"
                };
                ctx.Museums.Add(museum);
                ctx.SaveChanges();
                return museum.Id;
            }

            var rand = new Random(Guid.NewGuid().GetHashCode());
            return ids[rand.Next(ids.Count)];
        }

        private string CreateUniqueTicketTypeName(AppDbContext ctx, int museumId, string basePrefix = "AppendOnly Ticket")
        {
            string candidate;
            do
            {
                candidate = Unique(basePrefix);
            } while (ctx.TicketTypes.Any(t => t.MuseumId == museumId && t.Name == candidate));
            return candidate;
        }
        private decimal GetRandomPrice()
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            var cents = rnd.Next(1000, 5001);
            return Math.Round(cents / 100m, 2, MidpointRounding.AwayFromZero);
        }

        [Test]
        public void Always_Adds_New_TicketType_With_Unique_Name_And_Random_Price()
        {
            var museumId = GetOrCreateMuseumId();
            string uniqueName;
            decimal price;
            using (var ctx = CreateContext())
            {
                uniqueName = CreateUniqueTicketTypeName(ctx, museumId);
            }
            price = GetRandomPrice();
            using (var writeCtx = CreateContext())
            {
                var entity = new TicketType
                {
                    Name = uniqueName,
                    Price = price,
                    Description = "Append-only test record",
                    MuseumId = museumId
                };

                writeCtx.TicketTypes.Add(entity);
                writeCtx.SaveChanges();

                Assert.That(entity.Id, Is.GreaterThan(0), "Očekivan generisan Id nakon upisa.");
            }
            using (var readCtx = CreateContext())
            {
                var exists = readCtx.TicketTypes.Any(t =>
                    t.MuseumId == museumId &&
                    t.Name == uniqueName &&
                    t.Price == price
                );

                Assert.That(exists, Is.True, "TicketType sa očekivanom cenom bi trebalo da postoji u SQLite bazi nakon testa.");
            }
        }
    }
}
