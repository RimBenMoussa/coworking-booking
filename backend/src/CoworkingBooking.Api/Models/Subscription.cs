namespace CoworkingBooking.Api.Models;

public class Subscription
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";

    // Dates locales (Africa/Casablanca), bornes incluses.
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public SubscriptionStatus Status { get; set; }
    public decimal Price { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }

    public bool IsValidOn(DateOnly day) =>
        Status == SubscriptionStatus.Active && StartDate <= day && day <= EndDate;
}
