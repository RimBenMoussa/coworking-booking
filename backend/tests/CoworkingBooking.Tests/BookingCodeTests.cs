using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.Models;
using CoworkingBooking.Api.Services;
using static CoworkingBooking.Tests.TestEnvironment;

namespace CoworkingBooking.Tests;

public class BookingCodeTests
{
    // Générateur prévisible : renvoie les codes donnés dans l'ordre, puis répète le dernier.
    private sealed class FixedCodes(params string[] codes) : IBookingCodeGenerator
    {
        private int _index;
        public string Generate() => codes[Math.Min(_index++, codes.Length - 1)];
    }

    [Fact]
    public void Code_HasTheExpectedFormat()
    {
        var code = new BookingCodeGenerator().Generate();

        Assert.Matches("^CW-[23456789ABCDEFGHJKLMNPQRSTUVWXYZ]{5}$", code);
    }

    [Fact]
    public void Codes_NeverContainAmbiguousCharacters()
    {
        var generator = new BookingCodeGenerator();
        var characters = string.Concat(Enumerable.Range(0, 2000).Select(_ => generator.Generate()[3..]));

        Assert.DoesNotContain('0', characters);
        Assert.DoesNotContain('O', characters);
        Assert.DoesNotContain('1', characters);
        Assert.DoesNotContain('I', characters);
    }

    [Fact]
    public void Codes_AreRandom()
    {
        var generator = new BookingCodeGenerator();
        var codes = Enumerable.Range(0, 10_000).Select(_ => generator.Generate()).ToList();

        // 33 millions de codes possibles : quelques rares doublons sont possibles sur 10 000 tirages,
        // c'est pour cela que BookingService vérifie l'unicité en base.
        Assert.True(codes.Distinct().Count() > 9_990);
    }

    [Fact]
    public async Task Service_SkipsCodesAlreadyUsed()
    {
        using var env = new TestEnvironment();
        env.AddBooking(SeedData.OpenSpaceId, "09:00", BookingStatus.Terminee, code: "CW-AAAAA");
        env.CodeGenerator = new FixedCodes("CW-AAAAA", "CW-BBBBB");

        var booking = await env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId));

        Assert.Equal("CW-BBBBB", booking.Code);
    }

    [Fact]
    public async Task Service_GivesUp_WhenNoUniqueCodeCanBeFound()
    {
        using var env = new TestEnvironment();
        env.AddBooking(SeedData.OpenSpaceId, "09:00", BookingStatus.Terminee, code: "CW-AAAAA");
        env.CodeGenerator = new FixedCodes("CW-AAAAA");

        await Assert.ThrowsAsync<InvalidOperationException>(() => env.Bookings.CreateAsync(Request(SeedData.OpenSpaceId)));
    }
}
