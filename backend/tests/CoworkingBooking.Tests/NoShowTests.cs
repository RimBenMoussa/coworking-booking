using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.Models;
using CoworkingBooking.Api.Services;
using static CoworkingBooking.Tests.TestEnvironment;

namespace CoworkingBooking.Tests;

// Passage automatique en NonPresente 15 minutes après l'heure d'arrivée prévue.
public class NoShowTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task Booking_IsKeptDuringTheFifteenMinutes()
    {
        var booking = _env.AddBooking(SeedData.OpenSpaceId, "09:00", BookingStatus.Confirmee);
        _env.SetNow("09:15");

        var count = await _env.NoShow.MarkNoShowsAsync();

        Assert.Equal(0, count);
        Assert.Equal(BookingStatus.Confirmee, booking.Status);
    }

    [Fact]
    public async Task Booking_BecomesNoShow_AfterFifteenMinutes()
    {
        var booking = _env.AddBooking(SeedData.OpenSpaceId, "09:00", BookingStatus.Confirmee);
        _env.Clock.SetUtcNow(At("09:15").AddSeconds(1));

        var count = await _env.NoShow.MarkNoShowsAsync();

        Assert.Equal(1, count);
        Assert.Equal(BookingStatus.NonPresente, booking.Status);
    }

    [Theory]
    [InlineData(BookingStatus.Arrivee)]
    [InlineData(BookingStatus.DevisDemande)]
    [InlineData(BookingStatus.Annulee)]
    [InlineData(BookingStatus.Terminee)]
    public async Task OtherStatuses_AreNotTouched(BookingStatus status)
    {
        var booking = _env.AddBooking(SeedData.OpenSpaceId, "09:00", status);
        _env.SetNow("10:00");

        await _env.NoShow.MarkNoShowsAsync();

        Assert.Equal(status, booking.Status);
    }

    [Fact]
    public async Task NoShow_ReleasesThePlace()
    {
        // La Terrasse est pleine pour 09:00...
        for (var i = 0; i < 8; i++)
            _env.AddBooking(SeedData.TerrasseId, "09:00", BookingStatus.Confirmee);
        _env.SetNow("09:20");
        await Assert.ThrowsAsync<BusinessException>(() => _env.Bookings.CreateAsync(Request(SeedData.TerrasseId, "09:20")));

        // ... personne n'est venu : après le passage du service, les places sont libérées.
        await _env.NoShow.MarkNoShowsAsync();
        var booking = await _env.Bookings.CreateAsync(Request(SeedData.TerrasseId, "09:20"));

        Assert.Equal(BookingStatus.Confirmee, booking.Status);
    }

    [Fact]
    public async Task CheckIn_AfterFifteenMinutes_IsRefusedAndMarksNoShow()
    {
        var booking = _env.AddBooking(SeedData.OpenSpaceId, "09:00", BookingStatus.Confirmee);
        _env.SetNow("09:16");

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _env.FrontDesk.CheckInAsync(booking.Code));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(BookingStatus.NonPresente, booking.Status);
    }
}
