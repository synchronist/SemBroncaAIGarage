namespace SemBroncaAI.Garage.Web.Models;

public sealed record ApiTokenResponse(
    string TokenType,
    string AccessToken,
    long ExpiresIn);

public sealed record CurrentUserModel(
    Guid UserId,
    string Name,
    string? Email,
    string? Username,
    Guid? GarageId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string>? Permissions = null)
{
    public IReadOnlyCollection<string> EffectivePermissions => Permissions ?? [];
}

public sealed record ApiSession(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    CurrentUserModel User);

public sealed record AuthErrorModel(string Message, string? Code = null);
public sealed record PasswordResetErrorModel(string Code, IReadOnlyCollection<string> Messages);
