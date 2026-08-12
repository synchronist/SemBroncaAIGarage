using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class ApprovalHardeningTests
{
    [Fact]
    public void Public_Approval_Rate_Limit_Should_Reject_With_429()
    {
        var options = new RateLimiterOptions();
        ApprovalRateLimiting.Configure(options);
        options.RejectionStatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public void RespondedAt_Should_Be_An_Optimistic_Concurrency_Token()
    {
        var options = new DbContextOptionsBuilder<GarageDbContext>()
            .UseNpgsql("Host=localhost;Database=model-only;Username=test;Password=test")
            .Options;
        using var context = new GarageDbContext(options);
        var property = context.Model.FindEntityType(typeof(ServiceOrderEstimateApprovalEntity))!
            .FindProperty(nameof(ServiceOrderEstimateApprovalEntity.RespondedAt))!;
        property.IsConcurrencyToken.ShouldBeTrue();
    }
}
