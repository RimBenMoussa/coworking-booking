using CoworkingBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoworkingBooking.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Space>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(100);
            e.HasIndex(s => s.Name).IsUnique();
            e.Property(s => s.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.Description).HasMaxLength(500);
            e.ToTable(t => t.HasCheckConstraint("ck_spaces_capacity_positive", "capacity > 0"));
        });

        modelBuilder.Entity<Tariff>(e =>
        {
            e.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Amount).HasPrecision(8, 2);
            e.HasIndex(t => new { t.SpaceId, t.Type }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("ck_tariffs_amount_not_negative", "amount >= 0"));
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.Property(b => b.Code).HasMaxLength(8);
            e.HasIndex(b => b.Code).IsUnique();
            e.Property(b => b.FullName).HasMaxLength(100);
            e.Property(b => b.Phone).HasMaxLength(10);
            e.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(b => b.PaymentMethod).HasConversion<string>().HasMaxLength(10);
            e.Property(b => b.QuoteAmount).HasPrecision(8, 2);
            e.Property(b => b.AmountDue).HasPrecision(8, 2);
            e.Property(b => b.ManagerNote).HasMaxLength(500);
            // Index utilisé par le calcul de disponibilité et par le passage en NonPresente.
            e.HasIndex(b => new { b.SpaceId, b.Status, b.PlannedArrival });
            e.HasIndex(b => b.Phone);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_bookings_end_after_start", "planned_end > planned_arrival");
                t.HasCheckConstraint("ck_bookings_persons_positive", "persons > 0");
            });
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.Property(s => s.FullName).HasMaxLength(100);
            e.Property(s => s.Phone).HasMaxLength(10);
            e.Property(s => s.Email).HasMaxLength(200);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.Price).HasPrecision(8, 2);
            e.HasIndex(s => s.Phone);
        });

        modelBuilder.Entity<ContactMessage>(e =>
        {
            e.Property(m => m.Name).HasMaxLength(100);
            e.Property(m => m.Email).HasMaxLength(200);
            e.Property(m => m.Phone).HasMaxLength(10);
            e.Property(m => m.Message).HasMaxLength(2000);
        });

        modelBuilder.Entity<AdminUser>(e =>
        {
            e.Property(a => a.Email).HasMaxLength(200);
            e.HasIndex(a => a.Email).IsUnique();
        });

        SeedData.Apply(modelBuilder);
    }
}
