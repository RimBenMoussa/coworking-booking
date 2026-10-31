using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoworkingBooking.Api.Services;

public class SubscriptionService(AppDbContext db, TimeProvider clock, ILogger<SubscriptionService> logger)
{
    public async Task<Subscription> CreateAsync(CreateSubscriptionRequest request)
    {
        var phone = PhoneNumber.NormalizeOrThrow(request.Phone);
        var today = LocalTime.Today(clock);
        var start = request.StartDate!.Value;

        if (start < today)
            throw BusinessException.BadRequest("La date de début ne peut pas être dans le passé.");
        if (start > today.AddDays(BookingRules.MaxDaysAhead))
            throw BusinessException.BadRequest($"La date de début doit être dans les {BookingRules.MaxDaysAhead} prochains jours.");

        var existing = await db.Subscriptions.Where(s => s.Phone == phone).ToListAsync();
        if (existing.Any(s => s.Status == SubscriptionStatus.EnAttenteDePaiement))
            throw BusinessException.Conflict("Une demande d'abonnement est déjà en attente de paiement pour ce numéro.");
        if (existing.Any(s => s.Status == SubscriptionStatus.Active && s.EndDate >= start))
            throw BusinessException.Conflict("Un abonnement est déjà actif pour ce numéro à cette date.");

        var price = await db.Tariffs
            .Where(t => t.SpaceId == null && t.Type == TariffType.AbonnementMensuel)
            .Select(t => t.Amount)
            .FirstAsync();

        var subscription = new Subscription
        {
            FullName = request.FullName.Trim(),
            Phone = phone,
            Email = request.Email.Trim(),
            StartDate = start,
            EndDate = EndDateFor(start),
            Status = SubscriptionStatus.EnAttenteDePaiement,
            Price = price,
            CreatedAt = clock.GetUtcNow()
        };
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        logger.LogInformation("Demande d'abonnement {Id} pour le {Start}", subscription.Id, start);
        return subscription;
    }

    // Le gestionnaire active l'abonnement quand il encaisse le paiement.
    // Si la date de début est déjà passée, l'abonnement démarre le jour de l'activation (le client ne perd aucun jour).
    public async Task<Subscription> ActivateAsync(int id)
    {
        var subscription = await db.Subscriptions.FindAsync(id)
            ?? throw BusinessException.NotFound("Abonnement introuvable.");
        if (subscription.Status == SubscriptionStatus.Active)
            throw BusinessException.Conflict("Cet abonnement est déjà actif.");

        var today = LocalTime.Today(clock);
        if (subscription.StartDate < today)
        {
            subscription.StartDate = today;
            subscription.EndDate = EndDateFor(today);
        }
        subscription.Status = SubscriptionStatus.Active;
        subscription.ActivatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync();

        logger.LogInformation("Abonnement {Id} activé du {Start} au {End}", id, subscription.StartDate, subscription.EndDate);
        return subscription;
    }

    public async Task<List<SubscriptionDto>> ListAsync(SubscriptionStatus? status)
    {
        var today = LocalTime.Today(clock);
        var query = db.Subscriptions.AsQueryable();
        if (status is not null) query = query.Where(s => s.Status == status);

        var list = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        return list.Select(s => new SubscriptionDto(
            s.Id, s.FullName, s.Phone, s.Email, s.StartDate, s.EndDate, s.Status,
            s.IsValidOn(today), s.Price, LocalTime.ToLocal(s.CreatedAt),
            s.ActivatedAt is { } a ? LocalTime.ToLocal(a) : null)).ToList();
    }

    public Task<bool> HasActiveSubscriptionAsync(string phone, DateOnly day) =>
        db.Subscriptions.AnyAsync(s =>
            s.Phone == phone && s.Status == SubscriptionStatus.Active && s.StartDate <= day && day <= s.EndDate);

    // Validité d'un mois, bornes incluses : du 1er novembre au 30 novembre.
    public static DateOnly EndDateFor(DateOnly start) => start.AddMonths(1).AddDays(-1);
}
