using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoworkingBooking.Api.Services;

// Espaces, tarifs et disponibilités.
public class CatalogService(AppDbContext db, AvailabilityService availability, ILogger<CatalogService> logger)
{
    public async Task<SpacesResponse> GetSpacesAsync()
    {
        var spaces = await db.Spaces.Include(s => s.Tariffs).OrderBy(s => s.SortOrder).ToListAsync();
        var subscriptionPrice = await db.Tariffs
            .Where(t => t.SpaceId == null && t.Type == TariffType.AbonnementMensuel)
            .Select(t => t.Amount).FirstOrDefaultAsync();
        return new SpacesResponse(spaces.Select(SpaceDto.From).ToList(), subscriptionPrice);
    }

    public async Task<AvailabilityDto> GetAvailabilityAsync(int spaceId, DateOnly date, TimeOnly time, int? durationHours)
    {
        var space = await db.Spaces.FindAsync(spaceId)
            ?? throw BusinessException.NotFound("Cet espace n'existe pas.");

        var start = LocalTime.ToUtc(date, time);
        var end = space.Type == SpaceType.PostePartage
            ? start + BookingRules.SharedDeskWindow
            : start.AddHours(Math.Clamp(durationHours ?? 1, 1, BookingRules.MaxRoomHours));

        var result = await availability.CheckAsync(space, start, end);
        var busy = space.Type == SpaceType.PostePartage
            ? []
            : (await availability.BusySlotsAsync(space.Id, date))
                .Select(s => new SlotDto(LocalTime.ToLocal(s.Start), LocalTime.ToLocal(s.End))).ToList();

        return new AvailabilityDto(space.Id, space.Name, space.Type,
            LocalTime.ToLocal(start), LocalTime.ToLocal(end),
            result.IsAvailable, result.RemainingPlaces, result.Capacity, busy);
    }

    public async Task<List<TariffDto>> GetPricesAsync()
    {
        var tariffs = await db.Tariffs.Include(t => t.Space)
            .OrderBy(t => t.SpaceId == null).ThenBy(t => t.SpaceId).ThenBy(t => t.Type)
            .ToListAsync();
        return tariffs.Select(TariffDto.From).ToList();
    }

    public async Task<List<TariffDto>> UpdatePricesAsync(List<UpdatePriceRequest> updates)
    {
        var ids = updates.Select(u => u.Id!.Value).ToList();
        var tariffs = await db.Tariffs.Where(t => ids.Contains(t.Id)).ToDictionaryAsync(t => t.Id);

        foreach (var update in updates)
        {
            if (!tariffs.TryGetValue(update.Id!.Value, out var tariff))
                throw BusinessException.NotFound($"Tarif {update.Id} introuvable.");
            if (tariff.Amount != update.Amount)
            {
                logger.LogInformation("Tarif {Id} ({Type}) modifié : {Old} DH -> {New} DH",
                    tariff.Id, tariff.Type, tariff.Amount, update.Amount);
                tariff.Amount = update.Amount!.Value;
            }
        }
        await db.SaveChangesAsync();
        return await GetPricesAsync();
    }
}
