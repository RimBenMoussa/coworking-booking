using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoworkingBooking.Api.Services;

// Passe en NonPresente les réservations confirmées sans check-in 15 minutes après l'heure prévue.
// La place est alors libérée (NonPresente ne compte plus dans la disponibilité).
public class NoShowService(AppDbContext db, TimeProvider clock, ILogger<NoShowService> logger)
{
    public async Task<int> MarkNoShowsAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        var deadline = now - BookingRules.HoldDelay;

        var late = await db.Bookings.Include(b => b.Space)
            .Where(b => b.Status == BookingStatus.Confirmee && b.PlannedArrival < deadline)
            .ToListAsync(cancellationToken);

        foreach (var booking in late)
        {
            booking.Status = BookingStatus.NonPresente;
            booking.UpdatedAt = now;
            logger.LogInformation("Réservation {Code} non présentée : {Space}, arrivée prévue {Arrival}",
                booking.Code, booking.Space?.Name, LocalTime.ToLocal(booking.PlannedArrival));
        }

        await db.SaveChangesAsync(cancellationToken);
        return late.Count;
    }
}

// Tâche de fond : appelle NoShowService chaque minute.
public class NoShowBackgroundService(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<NoShowBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), clock);
        do
        {
            try
            {
                // Un scope par passage : le DbContext ne doit pas vivre aussi longtemps que l'application.
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<NoShowService>();
                await service.MarkNoShowsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Base indisponible par exemple : on journalise et on réessaie à la minute suivante.
                logger.LogError(ex, "Échec de la vérification des non-présentés");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
