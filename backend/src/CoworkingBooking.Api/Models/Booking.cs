namespace CoworkingBooking.Api.Models;

public class Booking
{
    public int Id { get; set; }

    // Code court à présenter à l'accueil, par exemple CW-7K3P9.
    public string Code { get; set; } = "";

    public int SpaceId { get; set; }
    public Space? Space { get; set; }

    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public int Persons { get; set; } = 1;

    // Toutes les dates sont stockées en UTC (timestamptz).
    // Fenêtre d'occupation prévue : [PlannedArrival, PlannedEnd).
    public DateTimeOffset PlannedArrival { get; set; }
    public DateTimeOffset PlannedEnd { get; set; }

    public BookingStatus Status { get; set; }
    public bool IsSubscriber { get; set; }

    public DateTimeOffset? CheckInAt { get; set; }
    public DateTimeOffset? CheckOutAt { get; set; }

    // Salles privées : montant fixé par le gestionnaire en acceptant le devis.
    public decimal? QuoteAmount { get; set; }

    // Montant encaissé au départ (calculé par PricingService).
    public decimal? AmountDue { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }

    public string? ManagerNote { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
