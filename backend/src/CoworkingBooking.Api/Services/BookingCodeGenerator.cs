using System.Security.Cryptography;

namespace CoworkingBooking.Api.Services;

public interface IBookingCodeGenerator
{
    string Generate();
}

// Codes du type CW-7K3P9. L'alphabet exclut 0, O, 1 et I, faciles à confondre à l'oral.
// 32 caractères possibles sur 5 positions : 33 millions de codes.
public class BookingCodeGenerator : IBookingCodeGenerator
{
    public const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    public const int Length = 5;

    public string Generate()
    {
        var chars = new char[Length];
        for (var i = 0; i < Length; i++)
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return "CW-" + new string(chars);
    }
}
