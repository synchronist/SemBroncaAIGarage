using Microsoft.AspNetCore.DataProtection;
using SemBroncaAI.Garage.Api.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class ApprovalTokenServiceTests
{
    [Fact]
    public void Tokens_Should_Be_Random_Hashed_And_Recoverable_From_Protected_Value()
    {
        var service = new ApprovalTokenService(new EphemeralDataProtectionProvider());
        var first = service.Create(); var second = service.Create();
        first.Value.ShouldNotBe(second.Value);
        first.Value.Length.ShouldBeGreaterThanOrEqualTo(43);
        first.Hash.ShouldNotContain(first.Value);
        first.Hash.Length.ShouldBe(64);
        service.Unprotect(first.ProtectedValue).ShouldBe(first.Value);
        service.Hash(first.Value).ShouldBe(first.Hash);
    }
}
