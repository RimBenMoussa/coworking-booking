using CoworkingBooking.Api.Models;

namespace CoworkingBooking.Api.Services;

public record PriceResult(decimal Amount, string Detail);

// Calcul du prix au départ, à partir du temps réel entre check-in et check-out.
// Aucune dépendance à la base ni à l'horloge : tout est passé en paramètre, ce qui rend les tests simples.
public class PricingService
{
    public PriceResult Calculate(
        Space space,
        IEnumerable<Tariff> tariffs,
        DateTimeOffset checkIn,
        DateTimeOffset checkOut,
        int persons,
        bool isSubscriber,
        decimal? quoteAmount)
    {
        if (checkOut < checkIn)
            throw new ArgumentException("L'heure de départ est antérieure à l'heure d'arrivée.");

        var prices = tariffs.Where(t => t.SpaceId == space.Id).ToDictionary(t => t.Type, t => t.Amount);
        var duration = checkOut - checkIn;

        return space.Type switch
        {
            SpaceType.PostePartage => SharedDeskPrice(prices, checkIn, duration, isSubscriber),
            SpaceType.Cabine => CabinPrice(prices, duration, persons),
            SpaceType.SallePrivee => RoomPrice(quoteAmount),
            _ => throw new InvalidOperationException($"Type d'espace inconnu : {space.Type}")
        };
    }

    // Nuit : arrivée (heure locale) entre 20:00 et 05:59.
    public static bool IsNightArrival(DateTimeOffset checkIn)
    {
        var hour = LocalTime.ToLocal(checkIn).Hour;
        return hour >= 20 || hour < 6;
    }

    // Heures entamées : 1 h 00 = 1 h, 1 h 05 = 2 h. Les secondes sont ignorées. Minimum 1 h.
    public static int StartedHours(TimeSpan duration)
    {
        var minutes = (int)Math.Floor(duration.TotalMinutes);
        return Math.Max(1, (int)Math.Ceiling(minutes / 60.0));
    }

    private static PriceResult SharedDeskPrice(
        Dictionary<TariffType, decimal> prices, DateTimeOffset checkIn, TimeSpan duration, bool isSubscriber)
    {
        if (isSubscriber)
            return new PriceResult(0, "Abonné : accès illimité inclus");

        if (IsNightArrival(checkIn))
            return new PriceResult(Get(prices, TariffType.Nuit), "Tarif nuit (arrivée entre 20:00 et 05:59)");

        return duration >= BookingRules.DayThreshold
            ? new PriceResult(Get(prices, TariffType.Journee), "Tarif journée (6 h ou plus)")
            : new PriceResult(Get(prices, TariffType.Court), "Tarif court (moins de 6 h)");
    }

    private static PriceResult CabinPrice(Dictionary<TariffType, decimal> prices, TimeSpan duration, int persons)
    {
        var hours = StartedHours(duration);

        // La Duo est facturée par personne, les Solo par cabine.
        if (prices.TryGetValue(TariffType.HeureParPersonne, out var perPerson))
            return new PriceResult(hours * perPerson * persons,
                $"{hours} h entamée(s) × {perPerson} DH × {persons} personne(s)");

        var hourly = Get(prices, TariffType.Heure);
        return new PriceResult(hours * hourly, $"{hours} h entamée(s) × {hourly} DH");
    }

    private static PriceResult RoomPrice(decimal? quoteAmount)
    {
        if (quoteAmount is null)
            throw new InvalidOperationException("Aucun montant de devis pour cette salle.");
        return new PriceResult(quoteAmount.Value, "Montant du devis");
    }

    private static decimal Get(Dictionary<TariffType, decimal> prices, TariffType type) =>
        prices.TryGetValue(type, out var amount)
            ? amount
            : throw new InvalidOperationException($"Tarif {type} manquant pour cet espace.");
}
