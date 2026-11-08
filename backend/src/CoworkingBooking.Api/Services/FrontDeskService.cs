using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoworkingBooking.Api.Services;

// Opérations du gestionnaire à l'accueil : liste du jour, occupation, check-in, check-out, devis.
public class FrontDeskService(
    AppDbContext db,
    AvailabilityService availability,
    SubscriptionService subscriptions,
    PricingService pricing,
    TimeProvider clock,
    ILogger<FrontDeskService> logger)
{
    // Sans date ni statut : les réservations du jour. Avec un statut seul : toutes les dates.
    public async Task<List<Booking>> ListAsync(DateOnly? date, BookingStatus? status)
    {
        var query = db.Bookings.Include(b => b.Space).AsQueryable();

        if (date is null && status is null)
            date = LocalTime.Today(clock);
        if (date is not null)
        {
            var (start, end) = LocalTime.DayBounds(date.Value);
            query = query.Where(b => b.PlannedArrival >= start && b.PlannedArrival < end);
        }
        if (status is not null)
            query = query.Where(b => b.Status == status);

        return await query.OrderBy(b => b.PlannedArrival).ToListAsync();
    }

    // Occupation à l'instant présent : personnes arrivées + réservations attendues dont la fenêtre couvre maintenant.
    public async Task<List<OccupancyDto>> OccupancyAsync()
    {
        var now = clock.GetUtcNow();
        var spaces = await db.Spaces.OrderBy(s => s.SortOrder).ToListAsync();
        var result = new List<OccupancyDto>();

        foreach (var space in spaces)
        {
            var current = await availability.Overlapping(space.Id, now, now.AddMinutes(1))
                .Select(b => b.Status).ToListAsync();
            var present = current.Count(s => s == BookingStatus.Arrivee);
            var expected = current.Count(s => s == BookingStatus.Confirmee);

            // Postes partagés : une unité par place. Cabines et salles : une seule unité (libre ou occupée).
            var units = space.Type == SpaceType.PostePartage ? space.Capacity : 1;
            var free = Math.Max(0, units - present - expected);
            result.Add(new OccupancyDto(space.Id, space.Name, space.Type, units, present, expected, free));
        }
        return result;
    }

    public async Task<Booking> CheckInAsync(string code)
    {
        var booking = await FindAsync(code);
        var now = clock.GetUtcNow();

        // Délai de 15 minutes dépassé : la place est libérée même si le service de fond n'est pas encore passé.
        if (booking.Status == BookingStatus.Confirmee && now > booking.PlannedArrival + BookingRules.HoldDelay)
        {
            booking.Status = BookingStatus.NonPresente;
            booking.UpdatedAt = now;
            await db.SaveChangesAsync();
            logger.LogInformation("Réservation {Code} non présentée (constaté au check-in)", booking.Code);
        }

        switch (booking.Status)
        {
            case BookingStatus.Confirmee:
                break;
            case BookingStatus.DevisDemande:
                throw BusinessException.Conflict("Le devis de cette salle n'a pas encore été validé.");
            case BookingStatus.NonPresente:
                throw BusinessException.Conflict(
                    "Le délai de 15 minutes est dépassé : la place a été libérée. Faites une nouvelle réservation.");
            default:
                throw BusinessException.Conflict(
                    $"Check-in impossible : la réservation est au statut « {BookingRules.Label(booking.Status)} ».");
        }

        if (now < booking.PlannedArrival - BookingRules.EarlyCheckIn)
            throw BusinessException.Conflict(
                $"Trop tôt : le check-in ouvre 30 minutes avant l'heure prévue ({LocalTime.ToLocal(booking.PlannedArrival):HH:mm}).");

        booking.Status = BookingStatus.Arrivee;
        booking.CheckInAt = now;
        booking.UpdatedAt = now;
        await db.SaveChangesAsync();

        logger.LogInformation("Check-in {Code} : {Space}", booking.Code, booking.Space!.Name);
        return booking;
    }

    // Aperçu du prix sans rien enregistrer, pour l'afficher avant l'encaissement.
    public async Task<CheckOutDto> PreviewCheckOutAsync(string code)
    {
        var booking = await FindAsync(code);
        var (price, minutes, _) = await ComputePriceAsync(booking);
        return new CheckOutDto(BookingDto.From(booking), minutes, price.Amount, price.Detail);
    }

    public async Task<CheckOutDto> CheckOutAsync(string code, PaymentMethod paymentMethod)
    {
        var booking = await FindAsync(code);
        var (price, minutes, isSubscriber) = await ComputePriceAsync(booking);
        var now = clock.GetUtcNow();

        booking.Status = BookingStatus.Terminee;
        booking.CheckOutAt = now;
        booking.IsSubscriber = isSubscriber;
        booking.AmountDue = price.Amount;
        booking.PaymentMethod = paymentMethod;
        booking.UpdatedAt = now;
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Check-out {Code} : {Space}, durée {Minutes} min, montant {Amount} DH ({Detail}), paiement {PaymentMethod}",
            booking.Code, booking.Space!.Name, minutes, price.Amount, price.Detail, paymentMethod);
        return new CheckOutDto(BookingDto.From(booking), minutes, price.Amount, price.Detail);
    }

    public async Task<Booking> AcceptQuoteAsync(string code, decimal amount, string? note)
    {
        var booking = await FindQuoteAsync(code);
        if (booking.PlannedArrival + BookingRules.HoldDelay < clock.GetUtcNow())
            throw BusinessException.Conflict("La date de cette demande est déjà passée.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        await availability.LockSpaceAsync(booking.SpaceId);

        // Aucun chevauchement avec une demande déjà validée.
        var result = await availability.CheckAsync(booking.Space!, booking.PlannedArrival, booking.PlannedEnd, booking.Id);
        if (!result.IsAvailable)
            throw BusinessException.Conflict($"{booking.Space!.Name} est déjà réservée sur ce créneau par un devis validé.");

        booking.Status = BookingStatus.Confirmee;
        booking.QuoteAmount = amount;
        booking.ManagerNote = note;
        booking.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        logger.LogInformation("Devis accepté {Code} : {Space}, {Amount} DH", booking.Code, booking.Space!.Name, amount);
        return booking;
    }

    public async Task<Booking> RejectQuoteAsync(string code, string? note)
    {
        var booking = await FindQuoteAsync(code);
        booking.Status = BookingStatus.Refusee;
        booking.ManagerNote = note;
        booking.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync();

        logger.LogInformation("Devis refusé {Code} : {Space}", booking.Code, booking.Space!.Name);
        return booking;
    }

    private async Task<(PriceResult Price, int Minutes, bool IsSubscriber)> ComputePriceAsync(Booking booking)
    {
        if (booking.Status != BookingStatus.Arrivee || booking.CheckInAt is null)
            throw BusinessException.Conflict(
                $"Check-out impossible : la réservation est au statut « {BookingRules.Label(booking.Status)} ».");

        var now = clock.GetUtcNow();
        var checkIn = booking.CheckInAt.Value;
        var tariffs = await db.Tariffs.Where(t => t.SpaceId == booking.SpaceId).ToListAsync();

        // L'abonnement a pu être activé après la réservation : on revérifie le jour de l'arrivée.
        var isSubscriber = booking.IsSubscriber || await subscriptions.HasActiveSubscriptionAsync(
            booking.Phone, DateOnly.FromDateTime(LocalTime.ToLocal(checkIn).DateTime));

        var price = pricing.Calculate(booking.Space!, tariffs, checkIn, now, booking.Persons, isSubscriber, booking.QuoteAmount);
        return (price, (int)(now - checkIn).TotalMinutes, isSubscriber);
    }

    private async Task<Booking> FindAsync(string code) =>
        await db.Bookings.Include(b => b.Space).FirstOrDefaultAsync(b => b.Code == code.ToUpperInvariant())
        ?? throw BusinessException.NotFound($"Aucune réservation avec le code {code}.");

    private async Task<Booking> FindQuoteAsync(string code)
    {
        var booking = await FindAsync(code);
        if (booking.Status != BookingStatus.DevisDemande)
            throw BusinessException.Conflict(
                $"Cette demande a déjà été traitée (statut : {BookingRules.Label(booking.Status)}).");
        return booking;
    }
}
