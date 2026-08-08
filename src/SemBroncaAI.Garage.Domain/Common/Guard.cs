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
}