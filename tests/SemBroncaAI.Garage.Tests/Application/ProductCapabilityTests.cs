using Microsoft.Extensions.Options;
using SemBroncaAI.Garage.Application.Features.ProductCapabilities;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Infrastructure.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application;

public sealed class ProductCapabilityTests
{
    [Theory]
    [InlineData(ProductCapability.AiAssistant)]
    [InlineData(ProductCapability.KnowledgeBase)]
    [InlineData(ProductCapability.AutomaticWhatsApp)]
    public void Future_capabilities_should_be_unavailable_for_current_standard_plan(ProductCapability capability)
    {
        var options = Options.Create(new ProductFeaturesOptions
        {
            AiAssistant = true,
            KnowledgeBase = true,
            AutomaticWhatsApp = true
        });
        var availability = new ProductCapabilityAvailability(options);

        availability.CanUse(capability, Guid.NewGuid(), SubscriptionPlan.Standard).ShouldBeFalse();
    }

    [Fact]
    public void Planned_ai_limits_should_remain_centralized()
    {
        SemBroncaAiLimits.AiActionsPerMonth.ShouldBe(500);
        SemBroncaAiLimits.WhatsAppNotificationsPerMonth.ShouldBe(1500);
        SemBroncaAiLimits.KnowledgeDocuments.ShouldBe(20);
        SemBroncaAiLimits.KnowledgeStorageMb.ShouldBe(100);
    }
}
