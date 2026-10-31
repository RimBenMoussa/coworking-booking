using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.Models;
using CoworkingBooking.Api.Services;
using static CoworkingBooking.Tests.TestEnvironment;

namespace CoworkingBooking.Tests;

public class PricingServiceTests
{
    private readonly PricingService _pricing = new();

    private static Space Space(int id) => SeedData.Spaces.Single(s => s.Id == id);

    private decimal Price(int spaceId, DateTimeOffset checkIn, DateTimeOffset checkOut,
        int persons = 1, bool subscriber = false, decimal? quote = null) =>
        _pricing.Calculate(Space(spaceId), SeedData.Tariffs, checkIn, checkOut, persons, subscriber, quote).Amount;

    // --- Postes partagés ---

    [Fact]
    public void OpenSpace_LessThanSixHours_IsShortRate()
    {
        Assert.Equal(25, Price(SeedData.OpenSpaceId, At("10:00"), At("15:59")));
    }

    [Fact]
    public void OpenSpace_ExactlySixHours_IsDayRate()
    {
        Assert.Equal(40, Price(SeedData.OpenSpaceId, At("09:00"), At("15:00")));
    }

    [Fact]
    public void Terrasse_UsesItsOwnRates()
    {
        Assert.Equal(30, Price(SeedData.TerrasseId, At("10:00"), At("12:00")));
        Assert.Equal(50, Price(SeedData.TerrasseId, At("08:00"), At("18:00")));
        Assert.Equal(50, Price(SeedData.TerrasseId, At("21:00"), At("23:00")));
    }

    [Fact]
    public void ArrivalAtExactly2000_IsNightRate()
    {
        Assert.Equal(40, Price(SeedData.OpenSpaceId, At("20:00"), At("21:00")));
    }

    [Fact]
    public void ArrivalAt1959_IsNotNightRate()
    {
        Assert.Equal(25, Price(SeedData.OpenSpaceId, At("19:59"), At("21:00")));
    }

    [Fact]
    public void ArrivalAt0559_IsNightRate()
    {
        Assert.Equal(40, Price(SeedData.OpenSpaceId, At("05:59"), At("07:00")));
    }

    [Fact]
    public void ArrivalAt0600_IsNotNightRate()
    {
        Assert.Equal(25, Price(SeedData.OpenSpaceId, At("06:00"), At("07:00")));
    }

    [Fact]
    public void NightRate_IsFlat_EvenForAWholeNight()
    {
        var nextMorning = At("08:00", Day.AddDays(1));
        Assert.Equal(40, Price(SeedData.OpenSpaceId, At("22:00"), nextMorning));
    }

    [Fact]
    public void Subscriber_PaysNothingForSharedDesks()
    {
        Assert.Equal(0, Price(SeedData.OpenSpaceId, At("09:00"), At("18:00"), subscriber: true));
        Assert.Equal(0, Price(SeedData.TerrasseId, At("21:00"), At("23:00"), subscriber: true));
    }

    // --- Cabines ---

    [Theory]
    [InlineData("10:05", 10)] // 5 min -> 1 h entamée
    [InlineData("11:00", 10)] // 1 h pile -> 1 h
    [InlineData("11:05", 20)] // 1 h 05 -> 2 h
    [InlineData("13:00", 30)] // 3 h pile -> 3 h
    public void SoloCabin_ChargesStartedHours(string checkOut, decimal expected)
    {
        Assert.Equal(expected, Price(SeedData.PhoneBoxSolo1Id, At("10:00"), At(checkOut)));
    }

    [Fact]
    public void SoloCabin_IgnoresSeconds()
    {
        var checkIn = At("10:00");
        Assert.Equal(10, Price(SeedData.PhoneBoxSolo2Id, checkIn, checkIn.AddMinutes(60).AddSeconds(40)));
    }

    [Fact]
    public void DuoCabin_ChargesPerPerson()
    {
        // 1 h 05 = 2 h entamées × 15 DH × 2 personnes
        Assert.Equal(60, Price(SeedData.PhoneBoxDuoId, At("10:00"), At("11:05"), persons: 2));
        Assert.Equal(15, Price(SeedData.PhoneBoxDuoId, At("10:00"), At("11:00"), persons: 1));
    }

    [Fact]
    public void Subscriber_StillPaysForCabins()
    {
        Assert.Equal(10, Price(SeedData.PhoneBoxSolo1Id, At("10:00"), At("10:30"), subscriber: true));
    }

    // --- Salles ---

    [Fact]
    public void Room_IsChargedTheQuoteAmount()
    {
        Assert.Equal(450, Price(SeedData.SalleNexusId, At("09:00"), At("12:00"), quote: 450));
        Assert.Equal(450, Price(SeedData.SalleNexusId, At("09:00"), At("12:00"), subscriber: true, quote: 450));
    }

    [Fact]
    public void Room_WithoutQuote_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Price(SeedData.SalleSparkId, At("09:00"), At("10:00")));
    }

    // --- Cas d'erreur ---

    [Fact]
    public void CheckOutBeforeCheckIn_Throws()
    {
        Assert.Throws<ArgumentException>(() => Price(SeedData.OpenSpaceId, At("10:00"), At("09:00")));
    }

    [Fact]
    public void MissingTariff_Throws()
    {
        var tariffsWithoutNight = SeedData.Tariffs.Where(t => t.Type != TariffType.Nuit);
        Assert.Throws<InvalidOperationException>(() =>
            _pricing.Calculate(Space(SeedData.OpenSpaceId), tariffsWithoutNight, At("22:00"), At("23:00"), 1, false, null));
    }

    [Fact]
    public void Result_ExplainsWhichRateWasApplied()
    {
        var result = _pricing.Calculate(Space(SeedData.OpenSpaceId), SeedData.Tariffs, At("20:00"), At("21:00"), 1, false, null);
        Assert.Contains("nuit", result.Detail);
    }
}
