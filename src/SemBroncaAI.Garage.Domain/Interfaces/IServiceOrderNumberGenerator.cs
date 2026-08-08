namespace SemBroncaAI.Garage.Domain.Interfaces;

public interface IServiceOrderNumberGenerator
{
    Task<int> GetNextAsync(
        Guid garageId,
        CancellationToken cancellationToken = default);
}