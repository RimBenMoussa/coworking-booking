using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Models;
using CoworkingBooking.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace CoworkingBooking.Tests;

// Base en mémoire (avec les espaces et tarifs du seed) et horloge contrôlée par le test.
// Chaque test crée son propre environnement : aucune donnée partagée entre tests.
public sealed class TestEnvironment : IDisposable
{
    // Lundi 2 novembre 2026, 09:00 à Fès.
    public static readonly DateOnly Day = new(2026, 11, 2);

    public FakeTimeProvider Clock { get; }
    public AppDbContext Db { get; }
    public IBookingCodeGenerator CodeGenerator { get; set; } = new BookingCodeGenerator();

    public TestEnvironment()
    {
        Clock = new FakeTimeProvider(At("09:00"));
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // La base en mémoire ne gère pas les transactions : on ignore l'avertissement.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();
    }

    // Heure locale de Fès -> instant UTC.
    public static DateTimeOffset At(string time, DateOnly? day = null) =>
        LocalTime.ToUtc(day ?? Day, TimeOnly.Parse(time));

    public void SetNow(string time, DateOnly? day = null) => Clock.SetUtcNow(At(time, day));

    public AvailabilityService Availability => new(Db, Clock);
    public SubscriptionService Subscriptions => new(Db, Clock, NullLogger<SubscriptionService>.Instance);

    public BookingService Bookings =>
        new(Db, Availability, Subscriptions, CodeGenerator, Clock, NullLogger<BookingService>.Instance);

    public FrontDeskService FrontDesk =>
        new(Db, Availability, Subscriptions, new PricingService(), Clock, NullLogger<FrontDeskService>.Instance);

    public NoShowService NoShow => new(Db, Clock, NullLogger<NoShowService>.Instance);

    public static CreateBookingRequest Request(int spaceId, string time = "09:00", string phone = "0612345678",
        int? persons = null, int? durationHours = null, DateOnly? day = null) => new()
    {
        SpaceId = spaceId,
        FullName = "Client Test",
        Phone = phone,
        Date = day ?? Day,
        Time = time,
        Persons = persons,
        DurationHours = durationHours
    };

    // Insère directement une réservation (sans passer par les règles), pour préparer un scénario.
    public Booking AddBooking(int spaceId, string time, BookingStatus status, int hours = 6, string? code = null)
    {
        var booking = new Booking
        {
            Code = code ?? CodeGenerator.Generate(),
            SpaceId = spaceId,
            FullName = "Client existant",
            Phone = "0700000000",
            PlannedArrival = At(time),
            PlannedEnd = At(time).AddHours(hours),
            Status = status,
            CreatedAt = Clock.GetUtcNow(),
            UpdatedAt = Clock.GetUtcNow()
        };
        Db.Bookings.Add(booking);
        Db.SaveChanges();
        return booking;
    }

    public void Dispose() => Db.Dispose();
}
