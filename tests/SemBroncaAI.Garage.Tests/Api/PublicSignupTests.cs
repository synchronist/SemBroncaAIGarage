using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using SemBroncaAI.Garage.Api.Controllers;
using SemBroncaAI.Garage.Application.Features.PlatformAdministration;
using SemBroncaAI.Garage.Domain.Entities;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class PublicSignupTests
{
    [Fact]
    public async Task Production_default_should_keep_public_signup_closed()
    {
        var service = new Signup();
        var controller = new PublicSignupController(service, Configuration(false));

        var result = await controller.Create(Command(), default);

        result.Result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(503);
        service.Called.ShouldBeFalse();
    }

    [Fact]
    public async Task Enabled_signup_should_delegate_to_shared_onboarding()
    {
        var service = new Signup();
        var result = await new PublicSignupController(service, Configuration(true)).Create(Command(), default);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<CreatePlatformGarageResponse>();
        service.Called.ShouldBeTrue();
        service.Command!.AcceptedTerms.ShouldBeTrue();
    }

    [Fact]
    public void Endpoint_should_be_anonymous_and_rate_limited()
    {
        typeof(PublicSignupController).GetCustomAttributes(typeof(AllowAnonymousAttribute), true).ShouldNotBeEmpty();
        var rateLimit = typeof(PublicSignupController).GetCustomAttributes(typeof(EnableRateLimitingAttribute), true)
            .Cast<EnableRateLimitingAttribute>().Single();
        rateLimit.PolicyName.ShouldBe("public-signup");
    }

    private static IConfiguration Configuration(bool enabled) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["PublicSignup:Enabled"] = enabled.ToString() })
        .Build();

    private static PublicGarageSignupCommand Command() => new("Oficina", "52998224725", "11999999999",
        "oficina@test.local", "Owner da Silva", "owner@test.local", true);

    private sealed class Signup : IPublicGarageSignup
    {
        public bool Called { get; private set; }
        public PublicGarageSignupCommand? Command { get; private set; }
        public Task<CreatePlatformGarageResponse> SignupAsync(PublicGarageSignupCommand command,
            CancellationToken cancellationToken = default)
        {
            Called = true; Command = command;
            return Task.FromResult(new CreatePlatformGarageResponse(Guid.NewGuid(), command.Name, true,
                InvitationDeliveryStatus.Created));
        }
    }
}
