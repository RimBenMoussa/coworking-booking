using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Models;
using CoworkingBooking.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static CoworkingBooking.Tests.TestEnvironment;

namespace CoworkingBooking.Tests;

public class SubscriptionServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    private static CreateSubscriptionRequest Request(DateOnly start, string phone = "0611223344") => new()
    {
        FullName = "Khadija Test", Phone = phone, Email = "k@test.ma", StartDate = start
    };

    [Fact]
    public async Task Create_IsPendingForOneMonthAtTheTariffPrice()
    {
        var subscription = await _env.Subscriptions.CreateAsync(Request(new DateOnly(2026, 11, 2)));

        Assert.Equal(SubscriptionStatus.EnAttenteDePaiement, subscription.Status);
        Assert.Equal(new DateOnly(2026, 12, 1), subscription.EndDate);
        Assert.Equal(800, subscription.Price);
    }

    [Fact]
    public async Task Create_InThePast_Returns400()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _env.Subscriptions.CreateAsync(Request(Day.AddDays(-1))));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Create_SecondPendingRequest_Returns409()
    {
        await _env.Subscriptions.CreateAsync(Request(Day));

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _env.Subscriptions.CreateAsync(Request(Day.AddDays(2))));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Activate_StartsTodayIfTheStartDateHasPassed()
    {
        var subscription = await _env.Subscriptions.CreateAsync(Request(Day));
        _env.SetNow("10:00", Day.AddDays(5));

        var active = await _env.Subscriptions.ActivateAsync(subscription.Id);

        Assert.Equal(SubscriptionStatus.Active, active.Status);
        Assert.Equal(Day.AddDays(5), active.StartDate);
        Assert.Equal(Day.AddDays(5).AddMonths(1).AddDays(-1), active.EndDate);
        Assert.True(await _env.Subscriptions.HasActiveSubscriptionAsync("0611223344", Day.AddDays(5)));
        Assert.False(await _env.Subscriptions.HasActiveSubscriptionAsync("0611223344", Day.AddDays(40)));
    }

    [Fact]
    public async Task Activate_Twice_Returns409()
    {
        var subscription = await _env.Subscriptions.CreateAsync(Request(Day));
        await _env.Subscriptions.ActivateAsync(subscription.Id);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _env.Subscriptions.ActivateAsync(subscription.Id));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task List_ShowsWhetherTheSubscriptionIsCurrentlyValid()
    {
        var pending = await _env.Subscriptions.CreateAsync(Request(Day, "0611111111"));
        var active = await _env.Subscriptions.CreateAsync(Request(Day, "0622222222"));
        await _env.Subscriptions.ActivateAsync(active.Id);

        var all = await _env.Subscriptions.ListAsync(null);
        var onlyPending = await _env.Subscriptions.ListAsync(SubscriptionStatus.EnAttenteDePaiement);

        Assert.True(all.Single(s => s.Id == active.Id).IsCurrentlyValid);
        Assert.False(all.Single(s => s.Id == pending.Id).IsCurrentlyValid);
        Assert.Equal(pending.Id, Assert.Single(onlyPending).Id);
    }
}

public class CatalogAndContactTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    private CatalogService Catalog => new(_env.Db, _env.Availability, NullLogger<CatalogService>.Instance);
    private ContactService Contact => new(_env.Db, _env.Clock, NullLogger<ContactService>.Instance);

    [Fact]
    public async Task Spaces_AreSeededWithTheirTariffs()
    {
        var response = await Catalog.GetSpacesAsync();

        Assert.Equal(8, response.Spaces.Count);
        Assert.Equal(800, response.SubscriptionPrice);
        Assert.Equal([25m, 40m, 40m], response.Spaces[0].Tariffs.Select(t => t.Amount));
        Assert.True(response.Spaces.Single(s => s.Name == "Salle Summit").OnQuote);
    }

    [Fact]
    public async Task UpdatePrices_ChangesTheAmountUsedForPricing()
    {
        await Catalog.UpdatePricesAsync([new UpdatePriceRequest { Id = 1, Amount = 30 }]);

        var booking = await _env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId));
        await _env.FrontDesk.CheckInAsync(booking.Code);
        _env.SetNow("10:00");
        var result = await _env.FrontDesk.CheckOutAsync(booking.Code, PaymentMethod.Especes);

        Assert.Equal(30, result.Amount);
    }

    [Fact]
    public async Task UpdatePrices_UnknownTariff_Returns404()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            Catalog.UpdatePricesAsync([new UpdatePriceRequest { Id = 999, Amount = 30 }]));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task Availability_ForACabin_ListsBusySlots()
    {
        _env.AddBooking(SeedData.PhoneBoxSolo1Id, "10:00", BookingStatus.Confirmee, hours: 1);

        var free = await Catalog.GetAvailabilityAsync(SeedData.PhoneBoxSolo1Id, Day, new TimeOnly(11, 0), 1);
        var taken = await Catalog.GetAvailabilityAsync(SeedData.PhoneBoxSolo1Id, Day, new TimeOnly(10, 30), 1);

        Assert.True(free.IsAvailable);
        Assert.False(taken.IsAvailable);
        Assert.Single(free.BusySlots);
    }

    [Fact]
    public async Task Contact_RequiresEmailOrPhone()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            Contact.CreateAsync(new CreateContactMessageRequest { Name = "Sara", Message = "Bonjour !" }));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Contact_IsStoredAndListed()
    {
        await Contact.CreateAsync(new CreateContactMessageRequest { Name = "Sara", Phone = "0612345678", Message = "Avez-vous une imprimante ?" });

        var messages = await Contact.ListAsync();

        Assert.Equal("Avez-vous une imprimante ?", Assert.Single(messages).Message);
    }
}

public class AuthServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly PasswordHasher<AdminUser> _hasher = new();

    public void Dispose() => _env.Dispose();

    private AuthService Auth => new(_env.Db, _hasher,
        Options.Create(new JwtOptions { Key = new string('k', 40) }), _env.Clock, NullLogger<AuthService>.Instance);

    public AuthServiceTests()
    {
        var admin = new AdminUser { Email = "gestion@test.ma" };
        admin.PasswordHash = _hasher.HashPassword(admin, "MotDePasse!");
        _env.Db.AdminUsers.Add(admin);
        _env.Db.SaveChanges();
    }

    [Fact]
    public async Task Login_WithGoodPassword_ReturnsAToken()
    {
        var response = await Auth.LoginAsync("Gestion@Test.ma ", "MotDePasse!");

        Assert.False(string.IsNullOrEmpty(response.Token));
        Assert.Equal(_env.Clock.GetUtcNow().AddHours(12), response.ExpiresAt);
    }

    [Theory]
    [InlineData("gestion@test.ma", "mauvais")]
    [InlineData("inconnu@test.ma", "MotDePasse!")]
    public async Task Login_WithBadCredentials_Returns401(string email, string password)
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => Auth.LoginAsync(email, password));
        Assert.Equal(401, ex.StatusCode);
    }
}
