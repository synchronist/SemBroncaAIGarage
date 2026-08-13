namespace SemBroncaAI.Garage.Application.Abstractions.Security;

public interface ICurrentUser
{
    Guid UserId { get; }
    Guid? GarageId { get; }
    bool IsAuthenticated { get; }
    bool IsPlatformAdmin { get; }
    IReadOnlyCollection<string> Roles { get; }
}

public interface ICurrentGarage
{
    Guid RequireGarageId();
}
