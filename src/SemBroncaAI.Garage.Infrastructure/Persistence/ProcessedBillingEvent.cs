namespace SemBroncaAI.Garage.Infrastructure.Persistence;

public sealed class ProcessedBillingEvent
{
    public string Id { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public DateTime ProcessedAt { get; private set; }

    private ProcessedBillingEvent() { }

    public ProcessedBillingEvent(string id, string type, DateTime processedAt)
    {
        Id = id;
        Type = type;
        ProcessedAt = processedAt;
    }
}
