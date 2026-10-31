using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoworkingBooking.Api.Services;

public record AvailabilityResult(bool IsAvailable, int RemainingPlaces, int Capacity);

public class AvailabilityService(AppDbContext db, TimeProvider clock)
{
    // Réservations actives (Confirmee ou Arrivee) qui chevauchent la plage [start, end).
    // Une personne déjà arrivée occupe sa place au moins jusqu'à maintenant, même si elle dépasse l'heure prévue.
    // Les réservations annulées, non présentées, terminées ou en attente de devis ne bloquent rien.
    public IQueryable<Booking> Overlapping(int spaceId, DateTimeOffset start, DateTimeOffset end, int? excludeBookingId = null)
    {
        var now = clock.GetUtcNow();
        return db.Bookings.Where(b =>
            b.SpaceId == spaceId
            && (b.Status == BookingStatus.Confirmee || b.Status == BookingStatus.Arrivee)
            && (excludeBookingId == null || b.Id != excludeBookingId)
            && b.PlannedArrival < end
            && (b.PlannedEnd > start || (b.Status == BookingStatus.Arrivee && now > start)));
    }

    public async Task<AvailabilityResult> CheckAsync(Space space, DateTimeOffset start, DateTimeOffset end, int? excludeBookingId = null)
    {
        var count = await Overlapping(space.Id, start, end, excludeBookingId).CountAsync();

        if (space.Type == SpaceType.PostePartage)
        {
            var remaining = Math.Max(0, space.Capacity - count);
            return new AvailabilityResult(remaining > 0, remaining, space.Capacity);
        }

        // Cabines et salles : un seul créneau à la fois.
        return new AvailabilityResult(count == 0, count == 0 ? 1 : 0, 1);
    }

    // Créneaux occupés d'une cabine ou d'une salle sur une journée locale.
    public async Task<List<(DateTimeOffset Start, DateTimeOffset End)>> BusySlotsAsync(int spaceId, DateOnly date)
    {
        var (dayStart, dayEnd) = LocalTime.DayBounds(date);
        var bookings = await Overlapping(spaceId, dayStart, dayEnd)
            .OrderBy(b => b.PlannedArrival)
            .Select(b => new { b.PlannedArrival, b.PlannedEnd })
            .ToListAsync();
        return bookings.Select(b => (b.PlannedArrival, b.PlannedEnd)).ToList();
    }

    // Verrou PostgreSQL sur l'espace, libéré à la fin de la transaction.
    // Deux réservations simultanées sur le même espace sont ainsi traitées l'une après l'autre :
    // la seconde voit la première et ne peut pas dépasser la capacité.
    public async Task LockSpaceAsync(int spaceId)
    {
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({spaceId})");
    }
}
