namespace SemBroncaAI.Garage.Infrastructure.Identity;

public sealed class DevelopmentIdentitySeedOptions
{
    public const string SectionName = "IdentitySeed";
    public bool Enabled { get; set; }
    public Guid GarageId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerUserName { get; set; } = string.Empty;
    public string OwnerPassword { get; set; } = string.Empty;
    public string ReceptionistName { get; set; } = "Receptionist Development";
    public string ReceptionistEmail { get; set; } = "receptionist@sbgarage.local";
    public string ReceptionistUserName { get; set; } = "receptionist";
    public string ReceptionistPassword { get; set; } = string.Empty;
    public string MechanicName { get; set; } = "Mechanic Development";
    public string MechanicEmail { get; set; } = "mechanic@sbgarage.local";
    public string MechanicUserName { get; set; } = "mechanic";
    public string MechanicPassword { get; set; } = string.Empty;
    public string PlatformAdminName { get; set; } = "PlatformAdmin Development";
    public string PlatformAdminEmail { get; set; } = "platformadmin@sbgarage.local";
    public string PlatformAdminUserName { get; set; } = "platformadmin";
    public string PlatformAdminPassword { get; set; } = string.Empty;
}
