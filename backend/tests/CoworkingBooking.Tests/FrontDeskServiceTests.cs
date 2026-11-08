using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.Models;
using CoworkingBooking.Api.Services;
using static CoworkingBooking.Tests.TestEnvironment;

namespace CoworkingBooking.Tests;

// Parcours à l'accueil : check-in, check-out, devis, occupation.
public class FrontDeskServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task DemoScenario_BookCheckInCheckOut_ComputesThePrice()
    {
        var booking = await _env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId, "09:00"));

        _env.SetNow("09:05");
        var checkedIn = await _env.FrontDesk.CheckInAsync(booking.Code);
        Assert.Equal(BookingStatus.Arrivee, checkedIn.Status);
        Assert.Equal(At("09:05"), checkedIn.CheckInAt);

        _env.SetNow("15:35");
        var preview = await _env.FrontDesk.PreviewCheckOutAsync(booking.Code);
        Assert.Equal(40, preview.Amount);
        Assert.Equal(BookingStatus.Arrivee, booking.Status); // l'aperçu n'enregistre rien

        var result = await _env.FrontDesk.CheckOutAsync(booking.Code, PaymentMethod.Carte);

        Assert.Equal(390, result.DurationMinutes);
        Assert.Equal(40, result.Amount);
        Assert.Equal(BookingStatus.Terminee, booking.Status);
        Assert.Equal(40, booking.AmountDue);
        Assert.Equal(PaymentMethod.Carte, booking.PaymentMethod);
        Assert.Equal(At("15:35"), booking.CheckOutAt);
    }

    [Fact]
    public async Task CheckIn_TooEarly_Returns409()
    {
        var booking = _env.AddBooking(SeedData.OpenSpaceId, "11:00", BookingStatus.Confirmee);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _env.FrontDesk.CheckInAsync(booking.Code));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CheckIn_ThirtyMinutesEarly_IsAccepted()
    {
        var booking = _env.AddBooking(SeedData.OpenSpaceId, "09:30", BookingStatus.Confirmee);

        var checkedIn = await _env.FrontDesk.CheckInAsync(booking.Code);

        Assert.Equal(BookingStatus.Arrivee, checkedIn.Status);
    }

    [Fact]
    public async Task CheckIn_UnknownCode_Returns404()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _env.FrontDesk.CheckInAsync("CW-ZZZZZ"));
        Assert.Equal(404, ex.StatusCode);
    }

    [Theory]
    [InlineData(BookingStatus.DevisDemande)]
    [InlineData(BookingStatus.Annulee)]
    [InlineData(BookingStatus.Arrivee)]
    public async Task CheckIn_WrongStatus_Returns409(BookingStatus status)
    {
        var booking = _env.AddBooking(SeedData.SalleSparkId, "09:00", status);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _env.FrontDesk.CheckInAsync(booking.Code));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CheckOut_WithoutCheckIn_Returns409()
    {
        var booking = _env.AddBooking(SeedData.OpenSpaceId, "09:00", BookingStatus.Confirmee);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _env.FrontDesk.CheckOutAsync(booking.Code, PaymentMethod.Especes));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CheckOut_SubscriptionActivatedAfterBooking_IsFree()
    {
        var booking = await _env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId));
        Assert.False(booking.IsSubscriber);

        // Le client s'abonne et paie à l'accueil en arrivant.
        await _env.FrontDesk.CheckInAsync(booking.Code);
        var subscription = await _env.Subscriptions.CreateAsync(new()
        {
            FullName = "Client Test", Phone = booking.Phone, Email = "client@test.ma", StartDate = Day
        });
        await _env.Subscriptions.ActivateAsync(subscription.Id);

        _env.SetNow("18:00");
        var result = await _env.FrontDesk.CheckOutAsync(booking.Code, PaymentMethod.Especes);

        Assert.Equal(0, result.Amount);
        Assert.True(booking.IsSubscriber);
    }

    [Fact]
    public async Task Quote_Accepted_ThenCheckOut_ChargesTheQuote()
    {
        var booking = await _env.Bookings.CreateAsync(Request(SeedData.SalleNexusId, "10:00", persons: 6, durationHours: 2));

        var accepted = await _env.FrontDesk.AcceptQuoteAsync(booking.Code, 300, "Café inclus");
        Assert.Equal(BookingStatus.Confirmee, accepted.Status);
        Assert.Equal(300, accepted.QuoteAmount);

        _env.SetNow("10:00");
        await _env.FrontDesk.CheckInAsync(booking.Code);
        _env.SetNow("12:30");
        var result = await _env.FrontDesk.CheckOutAsync(booking.Code, PaymentMethod.Carte);

        Assert.Equal(300, result.Amount);
    }

    [Fact]
    public async Task Quote_OverlappingAnAcceptedQuote_CannotBeAccepted()
    {
        var first = await _env.Bookings.CreateAsync(Request(SeedData.SalleNexusId, "14:00", persons: 6, durationHours: 2));
        var second = await _env.Bookings.CreateAsync(Request(SeedData.SalleNexusId, "15:00", persons: 4, durationHours: 2));
        await _env.FrontDesk.AcceptQuoteAsync(first.Code, 300, null);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _env.FrontDesk.AcceptQuoteAsync(second.Code, 300, null));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(BookingStatus.DevisDemande, second.Status);
    }

    [Fact]
    public async Task Quote_Rejected()
    {
        var booking = await _env.Bookings.CreateAsync(Request(SeedData.SalleSparkId, "14:00", persons: 2, durationHours: 1));

        var rejected = await _env.FrontDesk.RejectQuoteAsync(booking.Code, "Salle en travaux");

        Assert.Equal(BookingStatus.Refusee, rejected.Status);
        Assert.Equal("Salle en travaux", rejected.ManagerNote);
    }

    [Fact]
    public async Task Quote_AlreadyProcessed_Returns409()
    {
        var booking = await _env.Bookings.CreateAsync(Request(SeedData.SalleSparkId, "14:00", persons: 2, durationHours: 1));
        await _env.FrontDesk.RejectQuoteAsync(booking.Code, null);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _env.FrontDesk.AcceptQuoteAsync(booking.Code, 100, null));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Quote_InThePast_CannotBeAccepted()
    {
        var booking = await _env.Bookings.CreateAsync(Request(SeedData.SalleSparkId, "10:00", persons: 2, durationHours: 1));
        _env.SetNow("11:00");

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _env.FrontDesk.AcceptQuoteAsync(booking.Code, 100, null));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Occupancy_CountsPresentAndExpectedClients()
    {
        for (var i = 0; i < 10; i++)
            _env.AddBooking(SeedData.OpenSpaceId, "08:00", BookingStatus.Arrivee);
        _env.AddBooking(SeedData.OpenSpaceId, "09:00", BookingStatus.Confirmee);
        _env.AddBooking(SeedData.OpenSpaceId, "09:00", BookingStatus.Confirmee);
        _env.AddBooking(SeedData.OpenSpaceId, "09:00", BookingStatus.Annulee);
        _env.AddBooking(SeedData.OpenSpaceId, "13:00", BookingStatus.Confirmee); // plus tard
        _env.AddBooking(SeedData.PhoneBoxSolo1Id, "08:30", BookingStatus.Arrivee, hours: 1);

        var occupancy = await _env.FrontDesk.OccupancyAsync();

        var openSpace = occupancy.Single(o => o.SpaceId == SeedData.OpenSpaceId);
        Assert.Equal((17, 10, 2, 5), (openSpace.Units, openSpace.Present, openSpace.Expected, openSpace.Free));
        var cabin = occupancy.Single(o => o.SpaceId == SeedData.PhoneBoxSolo1Id);
        Assert.Equal((1, 1, 0), (cabin.Units, cabin.Present, cabin.Free));
        Assert.Equal(8, occupancy.Count);
    }

    [Fact]
    public async Task List_DefaultsToToday_AndFiltersByStatus()
    {
        _env.AddBooking(SeedData.OpenSpaceId, "10:00", BookingStatus.Confirmee);
        _env.AddBooking(SeedData.OpenSpaceId, "08:00", BookingStatus.Arrivee);
        var quote = _env.AddBooking(SeedData.SalleSparkId, "14:00", BookingStatus.DevisDemande);
        quote.PlannedArrival = At("14:00", Day.AddDays(3));
        quote.PlannedEnd = quote.PlannedArrival.AddHours(2);
        await _env.Db.SaveChangesAsync();

        var today = await _env.FrontDesk.ListAsync(null, null);
        var arrived = await _env.FrontDesk.ListAsync(Day, BookingStatus.Arrivee);
        var quotes = await _env.FrontDesk.ListAsync(null, BookingStatus.DevisDemande);

        Assert.Equal([At("08:00"), At("10:00")], today.Select(b => b.PlannedArrival));
        Assert.Single(arrived);
        Assert.Equal(quote.Code, Assert.Single(quotes).Code);
    }
}
