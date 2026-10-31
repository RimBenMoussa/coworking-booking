using CoworkingBooking.Api.Models;
using CoworkingBooking.Api.Services;

namespace CoworkingBooking.Api.DTOs;

// Les dates renvoyées sont en heure locale de Fès, avec leur décalage (ex. 2026-11-02T09:00:00+01:00).

public record TariffDto(int Id, int? SpaceId, string? SpaceName, TariffType Type, string Label, decimal Amount)
{
    public static TariffDto From(Tariff t) => new(t.Id, t.SpaceId, t.Space?.Name, t.Type, LabelOf(t.Type), t.Amount);

    public static string LabelOf(TariffType type) => type switch
    {
        TariffType.Court => "Moins de 6 h",
        TariffType.Journee => "Journée (6 h ou plus)",
        TariffType.Nuit => "Nuit (arrivée 20:00 – 05:59)",
        TariffType.Heure => "Par heure entamée",
        TariffType.HeureParPersonne => "Par heure entamée et par personne",
        TariffType.AbonnementMensuel => "Abonnement mensuel",
        _ => type.ToString()
    };
}

public record SpaceDto(
    int Id, string Name, SpaceType Type, int Capacity, string Description, bool OnQuote, List<TariffDto> Tariffs)
{
    public static SpaceDto From(Space s) => new(
        s.Id, s.Name, s.Type, s.Capacity, s.Description,
        s.Type == SpaceType.SallePrivee,
        s.Tariffs.OrderBy(t => t.Type).Select(TariffDto.From).ToList());
}

public record SpacesResponse(List<SpaceDto> Spaces, decimal SubscriptionPrice);

public record SlotDto(DateTimeOffset Start, DateTimeOffset End);

public record AvailabilityDto(
    int SpaceId, string SpaceName, SpaceType Type,
    DateTimeOffset Start, DateTimeOffset End,
    bool IsAvailable, int RemainingPlaces, int Capacity,
    List<SlotDto> BusySlots);

public record BookingDto(
    string Code,
    BookingStatus Status,
    int SpaceId,
    string SpaceName,
    SpaceType SpaceType,
    string FullName,
    string Phone,
    int Persons,
    DateTimeOffset PlannedArrival,
    DateTimeOffset PlannedEnd,
    DateTimeOffset HoldUntil,
    bool IsSubscriber,
    DateTimeOffset? CheckInAt,
    DateTimeOffset? CheckOutAt,
    decimal? QuoteAmount,
    decimal? AmountDue,
    PaymentMethod? PaymentMethod,
    string? ManagerNote,
    DateTimeOffset CreatedAt)
{
    public static BookingDto From(Booking b) => new(
        b.Code, b.Status, b.SpaceId, b.Space?.Name ?? "", b.Space?.Type ?? default,
        b.FullName, b.Phone, b.Persons,
        L(b.PlannedArrival), L(b.PlannedEnd), L(b.PlannedArrival + BookingRules.HoldDelay),
        b.IsSubscriber,
        b.CheckInAt is { } i ? L(i) : null,
        b.CheckOutAt is { } o ? L(o) : null,
        b.QuoteAmount, b.AmountDue, b.PaymentMethod, b.ManagerNote, L(b.CreatedAt));

    private static DateTimeOffset L(DateTimeOffset utc) => LocalTime.ToLocal(utc);
}

public record CheckOutDto(BookingDto Booking, int DurationMinutes, decimal Amount, string PriceDetail);

public record OccupancyDto(int SpaceId, string SpaceName, SpaceType Type, int Units, int Present, int Expected, int Free);

public record SubscriptionDto(
    int Id, string FullName, string Phone, string Email,
    DateOnly StartDate, DateOnly EndDate, SubscriptionStatus Status,
    bool IsCurrentlyValid, decimal Price, DateTimeOffset CreatedAt, DateTimeOffset? ActivatedAt);

public record ContactMessageDto(int Id, string Name, string? Email, string? Phone, string Message, DateTimeOffset CreatedAt)
{
    public static ContactMessageDto From(ContactMessage m) =>
        new(m.Id, m.Name, m.Email, m.Phone, m.Message, LocalTime.ToLocal(m.CreatedAt));
}

public record LoginResponse(string Token, DateTimeOffset ExpiresAt, string Email);
