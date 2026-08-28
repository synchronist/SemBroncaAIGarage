using SemBroncaAI.Garage.Domain.Entities.Garage;

namespace SemBroncaAI.Garage.Application.Features.ProductCapabilities;

public enum ProductCapability
{
    AiAssistant,
    KnowledgeBase,
    AutomaticWhatsApp
}

public interface IProductCapabilityAvailability
{
    bool CanUse(ProductCapability capability, Guid garageId, SubscriptionPlan plan);
}

public static class SemBroncaAiLimits
{
    public const int AiActionsPerMonth = 500;
    public const int WhatsAppNotificationsPerMonth = 1500;
    public const int KnowledgeDocuments = 20;
    public const int KnowledgeStorageMb = 100;
}
