using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

public sealed class ServiceOrderEstimateItemEntity : Entity
{
    public Guid EstimateId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public EstimateItemType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Total => Quantity * UnitPrice;

    private ServiceOrderEstimateItemEntity()
    {
    }

    internal ServiceOrderEstimateItemEntity(
        Guid estimateId,
        string description,
        EstimateItemType type,
        decimal quantity,
        decimal unitPrice)
    {
        EstimateId = Guard.AgainstEmpty(estimateId, nameof(estimateId));
        Description = Guard.AgainstNullOrWhiteSpace(description, nameof(description));

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "O tipo do item é inválido.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "A quantidade deve ser maior que zero.");
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "O valor unitário deve ser maior que zero.");
        }

        Type = type;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
