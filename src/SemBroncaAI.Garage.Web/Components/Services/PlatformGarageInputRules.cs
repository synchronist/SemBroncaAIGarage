using System.ComponentModel.DataAnnotations;

namespace SemBroncaAI.Garage.Web.Services;

public static class PlatformGarageInputRules
{
    public const string PasswordMessage = "A senha deve ter pelo menos 10 caracteres, incluindo letra maiúscula, minúscula, número e quatro caracteres distintos.";

    public static string NormalizeDocument(string value)
    {
        var trimmed = value.Trim();
        var digits = DigitsOnly(trimmed);
        return trimmed.All(character => char.IsDigit(character) || character is '.' or '/' or '-') ? digits : trimmed;
    }

    public static string NormalizePhone(string value) => DigitsOnly(value);

    public static bool IsValidPhone(string? value) => DigitsOnly(value ?? string.Empty).Length is 10 or 11;

    public static bool IsValidEmail(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maximumLength &&
        System.Net.Mail.MailAddress.TryCreate(value.Trim(), out _);

    public static bool IsValidUserName(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 100;

    public static bool PasswordMeetsPolicy(string? password) =>
        password is { Length: >= 10 } && password.Any(char.IsUpper) && password.Any(char.IsLower) &&
        password.Any(char.IsDigit) && password.Distinct().Count() >= 4;

    private static string DigitsOnly(string value) => new(value.Where(char.IsDigit).ToArray());
}

public sealed class BrazilianPhoneAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => PlatformGarageInputRules.IsValidPhone(value as string);
}

public sealed class PasswordPolicyAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => PlatformGarageInputRules.PasswordMeetsPolicy(value as string);
}

public sealed class ProductEmailAttribute(int maximumLength) : ValidationAttribute
{
    public override bool IsValid(object? value) => PlatformGarageInputRules.IsValidEmail(value as string, maximumLength);
}

public sealed class ProductUserNameAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => PlatformGarageInputRules.IsValidUserName(value as string);
}
