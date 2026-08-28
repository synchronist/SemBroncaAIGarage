using Microsoft.Extensions.Options;
using SemBroncaAI.Garage.Application.Features.ProductCapabilities;
using SemBroncaAI.Garage.Domain.Entities.Garage;

namespace SemBroncaAI.Garage.Infrastructure.Services;

public sealed class ProductFeaturesOptions
{
    public const string SectionName = "Features";
    public bool AiAssistant { get; set; }
    public bool KnowledgeBase { get; set; }
    public bool AutomaticWhatsApp { get; set; }
}

public sealed class ProductCapabilityAvailability(IOptions<ProductFeaturesOptions> options)
    : IProductCapabilityAvailability
{
    public bool CanUse(ProductCapability capability, Guid garageId, SubscriptionPlan plan)
    {
        if (garageId == Guid.Empty)
            return false;

        // No current commercial plan includes future AI capabilities. Keeping this
        // check explicit prevents a global flag from enabling them for Standard.
        if (!PlanIncludesCapability(plan, capability))
            return false;

        return capability switch
        {
            ProductCapability.AiAssistant => options.Value.AiAssistant,
            ProductCapability.KnowledgeBase => options.Value.KnowledgeBase,
            ProductCapability.AutomaticWhatsApp => options.Value.AutomaticWhatsApp,
            _ => false
        };
    }

    private static bool PlanIncludesCapability(SubscriptionPlan plan, ProductCapability capability) =>
        false;
}
