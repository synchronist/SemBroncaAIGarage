namespace SemBroncaAI.Garage.Api.Services;

public static class DocumentFileName
{
    public static string Create(string prefix, int number, string plate)
    {
        var decomposed = prefix.Normalize(System.Text.NormalizationForm.FormD);
        var safePrefix = new string(decomposed.Where(character => char.IsLetterOrDigit(character) &&
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).ToUpperInvariant();
        var safePlate = new string(plate.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return $"{safePrefix}-{number:D4}-{safePlate}.pdf";
    }
}
