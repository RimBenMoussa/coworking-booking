using System.Text.RegularExpressions;

namespace CoworkingBooking.Api.Services;

// Numéros mobiles marocains : 06XXXXXXXX ou 07XXXXXXXX.
// Accepte aussi les espaces, tirets, points et le préfixe +212 / 00212, puis les retire.
public static partial class PhoneNumber
{
    public const string InvalidMessage = "Numéro de téléphone invalide : format attendu 06XXXXXXXX ou 07XXXXXXXX.";

    [GeneratedRegex(@"^0[67]\d{8}$")]
    private static partial Regex MobileRegex();

    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var digits = new string(input.Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (digits.StartsWith("+212")) digits = "0" + digits[4..];
        else if (digits.StartsWith("00212")) digits = "0" + digits[5..];

        return MobileRegex().IsMatch(digits) ? digits : null;
    }

    public static string NormalizeOrThrow(string? input) =>
        Normalize(input) ?? throw BusinessException.BadRequest(InvalidMessage);
}
