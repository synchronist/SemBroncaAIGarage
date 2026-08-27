namespace SemBroncaAI.Garage.Infrastructure.Services;

public sealed class StripeBillingOptions
{
    public const string SectionName = "Stripe";
    public bool Enabled { get; set; }
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string MonthlyPriceId { get; set; } = string.Empty;
    public string AnnualPriceId { get; set; } = string.Empty;
}
