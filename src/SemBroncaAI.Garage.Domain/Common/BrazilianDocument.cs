namespace SemBroncaAI.Garage.Domain.Common;

public static class BrazilianDocument
{
    public static string Normalize(string? value) => string.Concat((value ?? string.Empty).Where(char.IsDigit));

    public static string Format(string? value)
    {
        var digits = Normalize(value);
        if (digits.Length > 14)
            digits = digits[..14];

        return digits.Length <= 11
            ? ApplyMask(digits, [3, 3, 3, 2], [".", ".", "-"])
            : ApplyMask(digits, [2, 3, 3, 4, 2], [".", ".", "/", "-"]);
    }

    public static bool IsValid(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            value.Any(character => !char.IsDigit(character) && character is not ('.' or '-' or '/') && !char.IsWhiteSpace(character)))
            return false;

        var digits = Normalize(value);
        return digits.Length switch { 0 => true, 11 => IsValidCpf(digits), 14 => IsValidCnpj(digits), _ => false };
    }

    private static string ApplyMask(string digits, int[] groups, string[] separators)
    {
        var result = new System.Text.StringBuilder();
        var offset = 0;

        for (var index = 0; index < groups.Length && offset < digits.Length; index++)
        {
            var length = Math.Min(groups[index], digits.Length - offset);
            result.Append(digits, offset, length);
            offset += length;

            if (offset < digits.Length && index < separators.Length)
                result.Append(separators[index]);
        }

        return result.ToString();
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
