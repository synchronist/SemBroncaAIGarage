namespace SemBroncaAI.Garage.Infrastructure.Persistence;

public sealed class ServiceOrderNumberSequence
{
    public Guid GarageId { get; private set; }
    public int LastNumber { get; private set; }
}
