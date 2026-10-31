namespace CoworkingBooking.Api.Services;

// Conversions entre UTC (stocké en base) et l'heure locale de Fès.
public static class LocalTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Casablanca");

    public static DateTimeOffset ToLocal(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, Zone);

    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(ToLocal(clock.GetUtcNow()).DateTime);

    // Date + heure saisies par le client (heure locale) -> instant UTC.
    public static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, Zone), TimeSpan.Zero);
    }

    // Début et fin (exclue) d'une journée locale, en UTC.
    public static (DateTimeOffset Start, DateTimeOffset End) DayBounds(DateOnly date) =>
        (ToUtc(date, TimeOnly.MinValue), ToUtc(date.AddDays(1), TimeOnly.MinValue));
}
