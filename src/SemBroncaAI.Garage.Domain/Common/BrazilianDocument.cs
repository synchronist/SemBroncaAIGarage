namespace SemBroncaAI.Garage.Domain.Common;

public static class BrazilianDocument
{
    public static string Normalize(string? value) => string.Concat((value ?? string.Empty).Where(char.IsDigit));
    public static bool IsValid(string? value)
    {
        var digits = Normalize(value);
        return digits.Length switch { 0 => true, 11 => IsValidCpf(digits), 14 => IsValidCnpj(digits), _ => false };
    }
    private static bool IsValidCpf(string value)
    {
        if (value.Distinct().Count() == 1) return false;
        return Digit(value, 9, 10) == value[9] - '0' && Digit(value, 10, 11) == value[10] - '0';
    }
    private static int Digit(string value, int length, int weight)
    {
        var sum = 0;
        for (var index = 0; index < length; index++) sum += (value[index] - '0') * (weight - index);
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
    private static bool IsValidCnpj(string value)
    {
        if (value.Distinct().Count() == 1) return false;
        int Calculate(int length)
        {
            var weights = length == 12 ? new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 } : new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            var sum = Enumerable.Range(0, length).Sum(index => (value[index] - '0') * weights[index]);
            var remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }
        return Calculate(12) == value[12] - '0' && Calculate(13) == value[13] - '0';
    }
}
