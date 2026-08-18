namespace SemBroncaAI.Garage.Domain.Common;

public static class Guard
{
    public static Guid AgainstEmpty(
        Guid value,
        string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador não pode ser vazio.",
                parameterName);
        }

        return value;
    }

    public static int AgainstZeroOrNegative(
        int value,
        string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "O valor deve ser maior que zero.");
        }

        return value;
    }

    public static string AgainstNullOrWhiteSpace(
        string? value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "O texto não pode ser vazio.",
                parameterName);
        }

        return value.Trim();
    }

    public static string AgainstMaximumLength(string value, int maximumLength, string parameterName)
    {
        if (value.Length > maximumLength)
            throw new ArgumentException($"O campo deve possuir no máximo {maximumLength} caracteres.", parameterName);
        return value;
    }

    public static string RequiredWithMaximumLength(string? value, int maximumLength, string parameterName) =>
        AgainstMaximumLength(AgainstNullOrWhiteSpace(value, parameterName), maximumLength, parameterName);

    public static string OptionalWithMaximumLength(string? value, int maximumLength, string parameterName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return AgainstMaximumLength(normalized, maximumLength, parameterName);
    }
}
