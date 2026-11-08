using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoworkingBooking.Api.Services;

// Opérations côté client : réserver, consulter, annuler.
public class BookingService(
    AppDbContext db,
    AvailabilityService availability,
    SubscriptionService subscriptions,
    IBookingCodeGenerator codeGenerator,
    TimeProvider clock,
    ILogger<BookingService> logger)
{
    // Tolérance pour une réservation faite au moment même de l'arrivée.
    private static readonly TimeSpan PastTolerance = TimeSpan.FromMinutes(5);

    public async Task<Booking> CreateAsync(CreateBookingRequest request)
    {
        var space = await db.Spaces.FindAsync(request.SpaceId!.Value)
            ?? throw BusinessException.NotFound("Cet espace n'existe pas.");

        var phone = PhoneNumber.NormalizeOrThrow(request.Phone);
        var start = LocalTime.ToUtc(request.Date!.Value, TimeOnly.Parse(request.Time));
        var now = clock.GetUtcNow();

        if (start < now - PastTolerance)
            throw BusinessException.BadRequest("L'heure d'arrivée est déjà passée.");
        if (start > now.AddDays(BookingRules.MaxDaysAhead))
            throw BusinessException.BadRequest($"On ne peut pas réserver plus de {BookingRules.MaxDaysAhead} jours à l'avance.");

        var (persons, end) = ValidatePersonsAndDuration(space, request, start);

        // Transaction + verrou : la vérification de place et l'insertion forment un tout.
        await using var transaction = await db.Database.BeginTransactionAsync();
        await availability.LockSpaceAsync(space.Id);

        var result = await availability.CheckAsync(space, start, end);
        if (!result.IsAvailable)
            throw BusinessException.Conflict(space.Type == SpaceType.PostePartage
                ? $"{space.Name} est complet à cette heure ({space.Capacity}/{space.Capacity} places réservées)."
                : $"{space.Name} est déjà réservée sur ce créneau. Choisissez une autre heure.");

        var booking = new Booking
        {
            Code = await GenerateUniqueCodeAsync(),
            SpaceId = space.Id,
            Space = space,
            FullName = request.FullName.Trim(),
            Phone = phone,
            Persons = persons,
            PlannedArrival = start,
            PlannedEnd = end,
            Status = space.Type == SpaceType.SallePrivee ? BookingStatus.DevisDemande : BookingStatus.Confirmee,
            IsSubscriber = await subscriptions.HasActiveSubscriptionAsync(phone, request.Date.Value),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        logger.LogInformation(
            "Réservation créée {Code} : {Space}, arrivée {Arrival}, statut {Status}, abonné {IsSubscriber}",
            booking.Code, space.Name, LocalTime.ToLocal(start), booking.Status, booking.IsSubscriber);
        return booking;
    }

    public async Task<Booking> GetAsync(string code, string? phone)
    {
        // Sans le bon téléphone, on répond "introuvable" pour ne rien révéler.
        var normalized = PhoneNumber.Normalize(phone);
        var booking = await db.Bookings.Include(b => b.Space)
            .FirstOrDefaultAsync(b => b.Code == code.ToUpperInvariant());

        if (booking is null || normalized is null || booking.Phone != normalized)
            throw BusinessException.NotFound("Aucune réservation ne correspond à ce code et ce téléphone.");
        return booking;
    }

    public async Task<Booking> CancelAsync(string code, string phone)
    {
        var booking = await GetAsync(code, phone);

        if (!BookingRules.CancellableStatuses.Contains(booking.Status))
            throw BusinessException.Conflict($"Cette réservation ne peut plus être annulée (statut : {BookingRules.Label(booking.Status)}).");

        booking.Status = BookingStatus.Annulee;
        booking.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync();

        logger.LogInformation("Réservation annulée {Code} : {Space}", booking.Code, booking.Space?.Name);
        return booking;
    }

    private static (int Persons, DateTimeOffset End) ValidatePersonsAndDuration(
        Space space, CreateBookingRequest request, DateTimeOffset start)
    {
        if (space.Type == SpaceType.PostePartage)
        {
            if (request.Persons is > 1)
                throw BusinessException.BadRequest("Une réservation de poste partagé correspond à une seule personne.");
            return (1, start + BookingRules.SharedDeskWindow);
        }

        var persons = request.Persons ?? 1;
        if (persons > space.Capacity)
            throw BusinessException.BadRequest($"{space.Name} accueille au maximum {space.Capacity} personne(s).");

        var maxHours = space.Type == SpaceType.Cabine ? BookingRules.MaxCabinHours : BookingRules.MaxRoomHours;
        if (request.DurationHours is not { } hours)
            throw BusinessException.BadRequest("La durée prévue (en heures) est obligatoire pour cet espace.");
        if (hours > maxHours)
            throw BusinessException.BadRequest($"La durée maximale pour {space.Name} est de {maxHours} heures.");

        return (persons, start.AddHours(hours));
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        // Les collisions sont très rares (33 millions de codes) ; on réessaie quelques fois par sécurité.
        // L'index unique sur Code reste le dernier garde-fou.
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = codeGenerator.Generate();
            if (!await db.Bookings.AnyAsync(b => b.Code == code))
                return code;
        }
        throw new InvalidOperationException("Impossible de générer un code de réservation unique.");
    }
}
