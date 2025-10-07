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
    public class AppendOnlyMuseumSqliteTests
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
        private string CreateUniqueMuseumName(AppDbContext ctx, string basePrefix = "AppendOnly Museum")
        {
            string candidate;
            do
            {
                candidate = Unique(basePrefix);
            } while (ctx.Museums.Any(m => m.Name == candidate));
            return candidate;
        }

        [Test]
        public void Always_Adds_New_Museum_With_Unique_Name()
        {
            string uniqueName;
            const string city = "Test City";
            using (var ctx = CreateContext())
            {
                uniqueName = CreateUniqueMuseumName(ctx);
            }
            using (var writeCtx = CreateContext())
            {
                var entity = new Museum
                {
                    Name = uniqueName,
                    City = city,
                    Description = "Append-only test record"
                };

                writeCtx.Museums.Add(entity);
                writeCtx.SaveChanges();

                Assert.That(entity.Id, Is.GreaterThan(0), "Očekivan generisan Id nakon upisa.");
            }
            using (var readCtx = CreateContext())
            {
                var exists = readCtx.Museums.Any(m => m.Name == uniqueName && m.City == city);
                Assert.That(exists, Is.True, "Muzej bi trebalo da postoji u SQLite bazi nakon testa.");
            }
        }
    }
}
