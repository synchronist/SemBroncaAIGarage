namespace SemBroncaAI.Garage.Infrastructure.Identity;

public static class ApplicationRoles
{
    public const string PlatformAdmin = "PlatformAdmin";
    public const string Owner = "Owner";
    public const string Receptionist = "Receptionist";
    public const string Mechanic = "Mechanic";

    public static readonly IReadOnlyCollection<string> All =
        [PlatformAdmin, Owner, Receptionist, Mechanic];
}
