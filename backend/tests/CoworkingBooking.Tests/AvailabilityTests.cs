using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.Models;
using CoworkingBooking.Api.Services;
using static CoworkingBooking.Tests.TestEnvironment;

namespace CoworkingBooking.Tests;

// Règles de disponibilité, vérifiées à travers la création de réservation.
public class AvailabilityTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    private void FillTerrasse(BookingStatus status = BookingStatus.Confirmee)
    {
        for (var i = 0; i < 8; i++)
            _env.AddBooking(SeedData.TerrasseId, "09:00", status);
    }

    [Fact]
    public async Task SharedDesk_FullCapacity_IsRefusedWith409()
    {
        FillTerrasse();

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _env.Bookings.CreateAsync(Request(SeedData.TerrasseId, "10:00")));

        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("complet", ex.Message);
    }

    [Fact]
    public async Task SharedDesk_LastPlace_IsAccepted()
    {
        for (var i = 0; i < 7; i++)
            _env.AddBooking(SeedData.TerrasseId, "09:00", BookingStatus.Confirmee);

        var booking = await _env.Bookings.CreateAsync(Request(SeedData.TerrasseId, "10:00"));

        Assert.Equal(BookingStatus.Confirmee, booking.Status);
        Assert.Equal(At("16:00"), booking.PlannedEnd); // fenêtre de 6 h
    }

    [Fact]
    public async Task SharedDesk_CheckedInClients_CountTowardsCapacity()
    {
        FillTerrasse(BookingStatus.Arrivee);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _env.Bookings.CreateAsync(Request(SeedData.TerrasseId, "10:00")));
    }

    [Theory]
    [InlineData(BookingStatus.Annulee)]
    [InlineData(BookingStatus.NonPresente)]
    [InlineData(BookingStatus.Terminee)]
    public async Task SharedDesk_ReleasedBookings_FreeThePlace(BookingStatus releasedStatus)
    {
        for (var i = 0; i < 7; i++)
            _env.AddBooking(SeedData.TerrasseId, "09:00", BookingStatus.Confirmee);
        _env.AddBooking(SeedData.TerrasseId, "09:00", releasedStatus);

        var booking = await _env.Bookings.CreateAsync(Request(SeedData.TerrasseId, "10:00"));

        Assert.Equal(BookingStatus.Confirmee, booking.Status);
    }

    [Fact]
    public async Task SharedDesk_AfterSixHourWindow_IsFreeAgain()
    {
        FillTerrasse(); // 09:00 -> 15:00

        var booking = await _env.Bookings.CreateAsync(Request(SeedData.TerrasseId, "15:00"));

        Assert.Equal(BookingStatus.Confirmee, booking.Status);
    }

    [Fact]
    public async Task SharedDesk_ClientStayingLonger_StillOccupiesThePlace()
    {
        // Arrivés à 02:00, fenêtre prévue jusqu'à 08:00, mais toujours là à 09:00.
        for (var i = 0; i < 8; i++)
            _env.AddBooking(SeedData.TerrasseId, "02:00", BookingStatus.Arrivee);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _env.Bookings.CreateAsync(Request(SeedData.TerrasseId, "09:00")));
    }

    [Fact]
    public async Task SharedDesk_ClientStayingLonger_DoesNotBlockLaterBookings()
    {
        for (var i = 0; i < 8; i++)
            _env.AddBooking(SeedData.TerrasseId, "02:00", BookingStatus.Arrivee);

        // Marge d'une heure après maintenant (09:00) : une arrivée à 10:00 est acceptée.
        var booking = await _env.Bookings.CreateAsync(Request(SeedData.TerrasseId, "10:00"));

        Assert.Equal(BookingStatus.Confirmee, booking.Status);
    }

    [Fact]
    public async Task Cabin_OverlappingSlot_IsRefusedWith409()
    {
        await _env.Bookings.CreateAsync(Request(SeedData.PhoneBoxSolo1Id, "10:00", durationHours: 2));

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _env.Bookings.CreateAsync(Request(SeedData.PhoneBoxSolo1Id, "11:00", phone: "0698765432", durationHours: 1)));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Cabin_AdjacentSlot_IsAccepted()
    {
        await _env.Bookings.CreateAsync(Request(SeedData.PhoneBoxSolo1Id, "10:00", durationHours: 1));

        var next = await _env.Bookings.CreateAsync(Request(SeedData.PhoneBoxSolo1Id, "11:00", durationHours: 1));

        Assert.Equal(BookingStatus.Confirmee, next.Status);
    }

    [Fact]
    public async Task Cabin_SameSlotInAnotherCabin_IsAccepted()
    {
        await _env.Bookings.CreateAsync(Request(SeedData.PhoneBoxSolo1Id, "10:00", durationHours: 1));

        var other = await _env.Bookings.CreateAsync(Request(SeedData.PhoneBoxSolo2Id, "10:00", durationHours: 1));

        Assert.Equal(BookingStatus.Confirmee, other.Status);
    }

    [Fact]
    public async Task Cabin_CancelledSlot_CanBeBookedAgain()
    {
        var first = await _env.Bookings.CreateAsync(Request(SeedData.PhoneBoxSolo1Id, "10:00", durationHours: 1));
        await _env.Bookings.CancelAsync(first.Code, first.Phone);

        var second = await _env.Bookings.CreateAsync(Request(SeedData.PhoneBoxSolo1Id, "10:00", durationHours: 1));

        Assert.Equal(BookingStatus.Confirmee, second.Status);
    }

    [Fact]
    public async Task Room_CreatesAQuoteRequest_ThatDoesNotBlockOtherRequests()
    {
        var first = await _env.Bookings.CreateAsync(Request(SeedData.SalleSparkId, "14:00", persons: 4, durationHours: 2));
        var second = await _env.Bookings.CreateAsync(Request(SeedData.SalleSparkId, "14:00", persons: 3, durationHours: 2));

        Assert.Equal(BookingStatus.DevisDemande, first.Status);
        Assert.Equal(BookingStatus.DevisDemande, second.Status);
    }

    [Fact]
    public async Task Room_AcceptedQuote_BlocksTheSlot()
    {
        _env.AddBooking(SeedData.SalleSparkId, "14:00", BookingStatus.Confirmee, hours: 2);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _env.Bookings.CreateAsync(Request(SeedData.SalleSparkId, "15:00", persons: 2, durationHours: 1)));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Availability_ReportsRemainingPlaces()
    {
        for (var i = 0; i < 5; i++)
            _env.AddBooking(SeedData.OpenSpaceId, "09:00", BookingStatus.Confirmee);
        var space = SeedData.Spaces.Single(s => s.Id == SeedData.OpenSpaceId);

        var result = await _env.Availability.CheckAsync(space, At("10:00"), At("16:00"));

        Assert.True(result.IsAvailable);
        Assert.Equal(12, result.RemainingPlaces);
    }

    [Fact]
    public async Task BusySlots_ListsOnlyActiveBookingsOfTheDay()
    {
        _env.AddBooking(SeedData.PhoneBoxDuoId, "10:00", BookingStatus.Confirmee, hours: 1);
        _env.AddBooking(SeedData.PhoneBoxDuoId, "12:00", BookingStatus.Annulee, hours: 1);
        _env.AddBooking(SeedData.PhoneBoxDuoId, "15:00", BookingStatus.Arrivee, hours: 2);

        var slots = await _env.Availability.BusySlotsAsync(SeedData.PhoneBoxDuoId, Day);

        Assert.Equal([At("10:00"), At("15:00")], slots.Select(s => s.Start));
    }
}
