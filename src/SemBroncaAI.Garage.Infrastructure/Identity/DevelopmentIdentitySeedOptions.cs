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
}
