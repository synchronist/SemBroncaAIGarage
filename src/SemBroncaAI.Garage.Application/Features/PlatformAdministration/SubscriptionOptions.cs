namespace SemBroncaAI.Garage.Application.Features.PlatformAdministration;

public sealed class SubscriptionOptions
{
    public const string SectionName = "Subscription";
    public int DefaultTrialDays { get; set; } = 10;
}
