using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

public sealed class ServiceOrderEstimateEntity : Entity
{
    private readonly List<ServiceOrderEstimateItemEntity> _items = [];

    public Guid ServiceOrderId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<ServiceOrderEstimateItemEntity> Items => _items.AsReadOnly();
    public decimal ServicesSubtotal => _items.Where(x => x.Type == EstimateItemType.Service).Sum(x => x.Total);
    public decimal PartsSubtotal => _items.Where(x => x.Type == EstimateItemType.Part).Sum(x => x.Total);
    public decimal Total => ServicesSubtotal + PartsSubtotal;
    public bool IsValid => _items.Count > 0;

    private ServiceOrderEstimateEntity()
    {
    }

    internal ServiceOrderEstimateEntity(
        Guid serviceOrderId,
        IEnumerable<ServiceOrderEstimateItemData> items)
    {
        ServiceOrderId = Guard.AgainstEmpty(serviceOrderId, nameof(serviceOrderId));
        CreatedAt = DateTimeOffset.UtcNow;
        ReplaceItems(items);
    }

    internal void Update(IEnumerable<ServiceOrderEstimateItemData> items)
    {
        ReplaceItems(items);
    }

    private void ReplaceItems(IEnumerable<ServiceOrderEstimateItemData> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var itemList = items.ToArray();

        if (itemList.Length == 0)
        {
            throw new InvalidOperationException("O orçamento deve possuir pelo menos um item.");
        }

        _items.Clear();
        _items.AddRange(itemList.Select(item => new ServiceOrderEstimateItemEntity(
            Id,
            item.Description,
            item.Type,
            item.Quantity,
            item.UnitPrice)));
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed record ServiceOrderEstimateItemData(
    string Description,
    EstimateItemType Type,
    decimal Quantity,
    decimal UnitPrice);
