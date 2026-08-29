using System.Text.RegularExpressions;

namespace SemBroncaAI.Garage.Domain.Common;

public static partial class BrazilianVehiclePlate
{
    public static string Normalize(string value) => Guard.AgainstNullOrWhiteSpace(value, nameof(value)).Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
    public static bool IsValid(string value) => PlatePattern().IsMatch(Normalize(value));

    [GeneratedRegex("^[A-Z]{3}(?:[0-9]{4}|[0-9][A-Z][0-9]{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex PlatePattern();
}
