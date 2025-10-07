using Microsoft.EntityFrameworkCore;
using MuseumTickets.Api.Domain;

namespace MuseumTickets.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Museum> Museums => Set<Museum>();
    public DbSet<Exhibition> Exhibitions => Set<Exhibition>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Museum>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.City).IsRequired().HasMaxLength(80);
            e.Property(x => x.Description).HasMaxLength(2000);
        });
        modelBuilder.Entity<Exhibition>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(160);
            e.Property(x => x.Description).HasMaxLength(2000);

            e.HasOne(x => x.Museum)
             .WithMany(m => m.Exhibitions)
             .HasForeignKey(x => x.MuseumId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<TicketType>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(80);
            e.Property(x => x.Description).HasMaxLength(500);

            e.HasOne(x => x.Museum)
             .WithMany()
             .HasForeignKey(x => x.MuseumId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Order>(e =>
        {
            e.Property(x => x.BuyerName).IsRequired().HasMaxLength(120);
            e.Property(x => x.BuyerEmail).HasMaxLength(120);

            e.HasOne(x => x.TicketType)
             .WithMany(t => t.Orders)
             .HasForeignKey(x => x.TicketTypeId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Exhibition)
             .WithMany()
             .HasForeignKey(x => x.ExhibitionId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
