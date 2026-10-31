using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.Models;
using CoworkingBooking.Api.Services;
using static CoworkingBooking.Tests.TestEnvironment;

namespace CoworkingBooking.Tests;

// Validation des demandes, consultation et annulation.
public class BookingServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    private async Task<int> BadRequest(Func<Task> action) =>
        (await Assert.ThrowsAsync<BusinessException>(action)).StatusCode;

    [Fact]
    public async Task Create_StoresNormalizedPhoneAndUtcTimes()
    {
        var request = Request(SeedData.OpenSpaceId, "10:30", phone: "+212 6 12-34-56-78");

        var booking = await _env.Bookings.CreateAsync(request);

        Assert.Equal("0612345678", booking.Phone);
        Assert.Equal(At("10:30"), booking.PlannedArrival);
        Assert.Equal(TimeSpan.Zero, booking.PlannedArrival.Offset);
        Assert.Matches("^CW-[2-9A-Z]{5}$", booking.Code);
    }

    [Fact]
    public async Task Create_UnknownSpace_Returns404()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _env.Bookings.CreateAsync(Request(99)));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidPhone_Returns400()
    {
        Assert.Equal(400, await BadRequest(() => _env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId, phone: "0512345678"))));
    }

    [Fact]
    public async Task Create_ArrivalInThePast_Returns400()
    {
        Assert.Equal(400, await BadRequest(() => _env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId, "08:00"))));
    }

    [Fact]
    public async Task Create_ArrivalTooFarAhead_Returns400()
    {
        var day = Day.AddDays(BookingRules.MaxDaysAhead + 1);
        Assert.Equal(400, await BadRequest(() => _env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId, day: day))));
    }

    [Fact]
    public async Task Create_SharedDeskForSeveralPeople_Returns400()
    {
        Assert.Equal(400, await BadRequest(() => _env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId, persons: 2))));
    }

    [Fact]
    public async Task Create_TooManyPeopleForCabin_Returns400()
    {
        Assert.Equal(400, await BadRequest(() =>
            _env.Bookings.CreateAsync(Request(SeedData.PhoneBoxDuoId, "10:00", persons: 3, durationHours: 1))));
    }

    [Fact]
    public async Task Create_CabinWithoutDuration_Returns400()
    {
        Assert.Equal(400, await BadRequest(() => _env.Bookings.CreateAsync(Request(SeedData.PhoneBoxSolo1Id, "10:00"))));
    }

    [Fact]
    public async Task Create_CabinTooLong_Returns400()
    {
        Assert.Equal(400, await BadRequest(() =>
            _env.Bookings.CreateAsync(Request(SeedData.PhoneBoxSolo1Id, "10:00", durationHours: 9))));
    }

    [Fact]
    public async Task Create_WithActiveSubscription_IsMarkedSubscriber()
    {
        _env.Db.Subscriptions.Add(new Subscription
        {
            FullName = "Abonnée", Phone = "0612345678", Email = "a@b.ma",
            StartDate = Day.AddDays(-3), EndDate = Day.AddDays(20), Status = SubscriptionStatus.Active
        });
        await _env.Db.SaveChangesAsync();

        var booking = await _env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId));

        Assert.True(booking.IsSubscriber);
    }

    [Fact]
    public async Task Create_WithPendingSubscription_IsNotMarkedSubscriber()
    {
        _env.Db.Subscriptions.Add(new Subscription
        {
            FullName = "Abonnée", Phone = "0612345678", Email = "a@b.ma",
            StartDate = Day, EndDate = Day.AddDays(29), Status = SubscriptionStatus.EnAttenteDePaiement
        });
        await _env.Db.SaveChangesAsync();

        var booking = await _env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId));

        Assert.False(booking.IsSubscriber);
    }

    [Fact]
    public async Task Get_WithCodeInLowerCaseAndInternationalPhone_FindsTheBooking()
    {
        var booking = await _env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId));

        var found = await _env.Bookings.GetAsync(booking.Code.ToLowerInvariant(), "+212612345678");

        Assert.Equal(booking.Id, found.Id);
    }

    [Fact]
    public async Task Get_WithWrongPhone_Returns404()
    {
        var booking = await _env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId));

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _env.Bookings.GetAsync(booking.Code, "0699999999"));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task Cancel_IsFreeAndChangesStatus()
    {
        var booking = await _env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId));

        var cancelled = await _env.Bookings.CancelAsync(booking.Code, booking.Phone);

        Assert.Equal(BookingStatus.Annulee, cancelled.Status);
        Assert.Null(cancelled.AmountDue);
    }

    [Fact]
    public async Task Cancel_QuoteRequest_IsAllowed()
    {
        var booking = await _env.Bookings.CreateAsync(Request(SeedData.SalleSummitId, "14:00", persons: 10, durationHours: 3));

        var cancelled = await _env.Bookings.CancelAsync(booking.Code, booking.Phone);

        Assert.Equal(BookingStatus.Annulee, cancelled.Status);
    }

    [Fact]
    public async Task Cancel_Twice_Returns409()
    {
        var booking = await _env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId));
        await _env.Bookings.CancelAsync(booking.Code, booking.Phone);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _env.Bookings.CancelAsync(booking.Code, booking.Phone));
        Assert.Equal(409, ex.StatusCode);
    }

    [Theory]
    [InlineData("0612345678", "0612345678")]
    [InlineData("07 12 34 56 78", "0712345678")]
    [InlineData("+212712345678", "0712345678")]
    [InlineData("00212612345678", "0612345678")]
    [InlineData("06.12.34.56.78", "0612345678")]
    [InlineData("0512345678", null)]
    [InlineData("061234567", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void PhoneNumber_Normalize(string? input, string? expected)
    {
        Assert.Equal(expected, PhoneNumber.Normalize(input));
    }
}
