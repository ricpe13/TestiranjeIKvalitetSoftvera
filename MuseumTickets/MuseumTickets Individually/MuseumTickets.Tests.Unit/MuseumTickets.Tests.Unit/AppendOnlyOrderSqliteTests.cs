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
    public class AppendOnlyOrderSqliteTests
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
        private int GetRandomMuseumIdOrCreate()
        {
            using var ctx = CreateContext();
            var ids = ctx.Museums.Select(m => m.Id).ToList();
            if (ids.Count == 0)
            {
                var museum = new Museum
                {
                    Name = Unique("AppendOnly Museum"),
                    City = "Test City",
                    Description = "Auto-created for Order append-only test"
                };
                ctx.Museums.Add(museum);
                ctx.SaveChanges();
                return museum.Id;
            }

            var rand = NewRandom();
            return ids[rand.Next(ids.Count)];
        }

        private int GetRandomTicketTypeIdOrCreate(int museumId)
        {
            using var ctx = CreateContext();
            var ids = ctx.TicketTypes.Where(t => t.MuseumId == museumId)
                                     .Select(t => t.Id)
                                     .ToList();

            if (ids.Count == 0)
            {
                var rand = NewRandom();
                var toCreate = rand.Next(1, 4);
                for (int i = 0; i < toCreate; i++)
                {
                    var tt = new TicketType
                    {
                        Name = Unique("AppendOnly Ticket"),
                        Price = Math.Round(rand.Next(1000, 5001) / 100m, 2, MidpointRounding.AwayFromZero),
                        Description = "Append-only TicketType",
                        MuseumId = museumId
                    };
                    ctx.TicketTypes.Add(tt);
                }
                ctx.SaveChanges();

                ids = ctx.TicketTypes.Where(t => t.MuseumId == museumId)
                                     .Select(t => t.Id)
                                     .ToList();
            }

            return ids[NewRandom().Next(ids.Count)];
        }

        private int GetRandomExhibitionIdOrCreate(int museumId)
        {
            using var ctx = CreateContext();
            var ids = ctx.Exhibitions.Where(e => e.MuseumId == museumId)
                                     .Select(e => e.Id)
                                     .ToList();

            if (ids.Count == 0)
            {
                var rand = NewRandom();
                var toCreate = rand.Next(1, 4);
                for (int i = 0; i < toCreate; i++)
                {
                    var ex = new Exhibition
                    {
                        Title = Unique("AppendOnly Exhibition"),
                        Description = "Append-only Exhibition",
                        StartDate = DateTime.UtcNow.Date,
                        EndDate = DateTime.UtcNow.Date.AddDays(rand.Next(7, 61)),
                        MuseumId = museumId
                    };
                    ctx.Exhibitions.Add(ex);
                }
                ctx.SaveChanges();

                ids = ctx.Exhibitions.Where(e => e.MuseumId == museumId)
                                     .Select(e => e.Id)
                                     .ToList();
            }

            return ids[NewRandom().Next(ids.Count)];
        }

        private int GetRandomQuantity(int min = 1, int max = 5)
        {
            var rand = NewRandom();
            return rand.Next(min, max + 1);
        }

        private static string SanitizeLocalPart(string s)
        {
            var local = s.ToLowerInvariant();
            foreach (var ch in new[] { " ", "\t", "\r", "\n", "\"", "'", "\\", "/", "(", ")", "<", ">", ",", ";", ":", "[", "]" })
                local = local.Replace(ch, "");
            local = local.Replace("..", ".");
            if (local.Length > 24) local = local[..24];
            return string.IsNullOrWhiteSpace(local) ? "user" : local;
        }

        private string CreateUniqueBuyerName(AppDbContext ctx, string basePrefix = "AppendOnly Buyer")
        {
            string candidate;
            do
            {
                candidate = Unique(basePrefix);
            } while (ctx.Orders.Any(o => o.BuyerName == candidate));
            return candidate;
        }
        private string CreateUniqueBuyerEmail(AppDbContext ctx, string buyerNameSeed)
        {
            var rand = NewRandom();
            var domains = new[] { "example.com", "test.com", "mail.test", "demo.test" };
            string candidate;
            var localBase = SanitizeLocalPart(buyerNameSeed);

            do
            {
                var suffix = rand.Next(1000, 10000);
                candidate = $"{localBase}{suffix}@{domains[rand.Next(domains.Length)]}";
            } while (ctx.Orders.Any(o => o.BuyerEmail == candidate));

            return candidate;
        }

        [Test]
        public void Always_Adds_New_Order_With_Randomized_Fields()
        {
            var museumId = GetRandomMuseumIdOrCreate();
            var ticketTypeId = GetRandomTicketTypeIdOrCreate(museumId);
            var exhibitionId = GetRandomExhibitionIdOrCreate(museumId);
            var quantity = GetRandomQuantity(1, 5);
            string buyerName;
            string buyerEmail;

            using (var ctx = CreateContext())
            {
                buyerName = CreateUniqueBuyerName(ctx);
                buyerEmail = CreateUniqueBuyerEmail(ctx, buyerName);
            }
            int newOrderId;
            using (var writeCtx = CreateContext())
            {
                var order = new Order
                {
                    BuyerName = buyerName,
                    BuyerEmail = buyerEmail,
                    Quantity = quantity,
                    OrderedAt = DateTime.UtcNow,
                    TicketTypeId = ticketTypeId,
                    ExhibitionId = exhibitionId
                };

                writeCtx.Orders.Add(order);
                writeCtx.SaveChanges();

                Assert.That(order.Id, Is.GreaterThan(0), "Očekivan generisan Id nakon upisa.");
                newOrderId = order.Id;
            }
            using (var readCtx = CreateContext())
            {
                var exists = readCtx.Orders.Any(o =>
                    o.Id == newOrderId &&
                    o.BuyerName == buyerName &&
                    o.BuyerEmail == buyerEmail &&
                    o.Quantity == quantity &&
                    o.TicketTypeId == ticketTypeId &&
                    o.ExhibitionId == exhibitionId
                );

                Assert.That(exists, Is.True, "Order bi trebalo da postoji u SQLite bazi nakon testa.");
            }
        }
    }
}
