namespace SemBroncaAI.Garage.Domain.Common;

public static class BrazilianPhone
{
    public static string Normalize(string? value) => string.Concat((value ?? string.Empty).Where(char.IsDigit));
    public static bool IsValid(string? value)
    {
        var digits = Normalize(value);
        return digits.Length is 10 or 11 && digits.Distinct().Count() > 1;
    }
}
