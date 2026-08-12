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
    IReadOnlyCollection<string> Roles);

public sealed record ApiSession(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    CurrentUserModel User);
