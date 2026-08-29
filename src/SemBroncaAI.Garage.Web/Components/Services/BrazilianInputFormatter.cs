using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Web.Services;

public static class BrazilianInputFormatter
{
    public static string Document(string? value) => BrazilianDocument.Format(value);

    public static string Cpf(string? value)
    {
        var digits = BrazilianDocument.Normalize(value);
        if (digits.Length > 11) digits = digits[..11];
        if (digits.Length <= 3) return digits;
        if (digits.Length <= 6) return $"{digits[..3]}.{digits[3..]}";
        if (digits.Length <= 9) return $"{digits[..3]}.{digits.Substring(3, 3)}.{digits[6..]}";
        return $"{digits[..3]}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits[9..]}";
    }

    public static string Phone(string? value)
    {
        var digits = BrazilianPhone.Normalize(value);
        if (digits.Length > 11) digits = digits[..11];
        if (digits.Length == 0) return string.Empty;
        if (digits.Length <= 2) return $"({digits}";
        if (digits.Length <= 6) return $"({digits[..2]}) {digits[2..]}";
        return digits.Length <= 10
            ? $"({digits[..2]}) {digits.Substring(2, 4)}-{digits[6..]}"
            : $"({digits[..2]}) {digits.Substring(2, 5)}-{digits[7..]}";
    }

    public static string Plate(string? value)
    {
        var characters = (value ?? string.Empty).Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant).Take(7).ToArray();
        var normalized = new string(characters);
        if (normalized.Length <= 3) return normalized;
        var oldPatternCandidate = normalized.Skip(3).All(char.IsDigit);
        return oldPatternCandidate ? $"{normalized[..3]}-{normalized[3..]}" : normalized;
    }
}
