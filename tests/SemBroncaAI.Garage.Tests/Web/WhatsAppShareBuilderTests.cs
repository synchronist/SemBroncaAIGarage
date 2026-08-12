using Microsoft.Extensions.Configuration;
using SemBroncaAI.Garage.Web.Models;
using SemBroncaAI.Garage.Web.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class WhatsAppShareBuilderTests
{
    private readonly WhatsAppShareBuilder _builder = new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["PublicAppBaseUrl"] = "https://garage.test/app/" })
        .Build());

    [Fact]
    public void Should_build_professional_message_without_internal_data()
    {
        var result = _builder.Build(Estimate(), "Oficina Palmeiras", "(13) 3333-4444", "http://localhost:5123/");

        result.Message.ShouldContain("Olá, João!");
        result.Message.ShouldContain("Volkswagen Gol");
        result.Message.ShouldContain("ABC1D23");
        result.Message.ShouldContain("OS #0017");
        result.Message.ShouldContain("R$ 625,00");
        result.Message.ShouldContain("https://garage.test/app/approval/public-token");
        result.Message.ShouldNotContain("InternalNotes");
        result.Message.ShouldNotContain(Estimate().ServiceOrderId.ToString());
    }

    [Theory]
    [InlineData("(13) 99999-8888", "5513999998888")]
    [InlineData("13 3333-4444", "551333334444")]
    [InlineData("+55 (13) 99999-8888", "5513999998888")]
    [InlineData("005513999998888", "5513999998888")]
    public void Should_normalize_brazilian_phone(string input, string expected) =>
        WhatsAppShareBuilder.NormalizeBrazilianPhone(input).ShouldBe(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("telefone inválido")]
    public void Should_return_null_for_missing_or_invalid_phone(string? input) =>
        WhatsAppShareBuilder.NormalizeBrazilianPhone(input).ShouldBeNull();

    [Fact]
    public void Should_join_public_base_url_without_duplicate_slashes()
    {
        var link = _builder.BuildApprovalLink("abc/123", "http://localhost:5123/");
        link.ShouldBe("https://garage.test/app/approval/abc%2F123");
        link.ShouldNotContain("test//");
    }

    [Fact]
    public void Should_use_the_current_web_origin_when_public_base_url_is_not_configured()
    {
        var builder = new WhatsAppShareBuilder(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PublicAppBaseUrl"] = "" })
            .Build());

        builder.BuildApprovalLink("token", "http://localhost:5123/")
            .ShouldBe("http://localhost:5123/approval/token");
    }

    [Fact]
    public void Internal_list_contract_should_not_contain_sensitive_approval_ids_or_notes()
    {
        var names = typeof(EstimateListItemModel).GetProperties().Select(x => x.Name).ToArray();
        names.ShouldNotContain("InternalNotes");
        names.ShouldNotContain("GarageId");
        names.ShouldNotContain("CustomerId");
        names.ShouldNotContain("VehicleId");
        names.ShouldNotContain("ApprovalId");
        names.ShouldNotContain("ProtectedToken");
    }

    private static EstimateListItemModel Estimate() => new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        17, "WaitingApproval", DateTimeOffset.UtcNow, "João da Silva", "(13) 99999-8888",
        "Volkswagen Gol", "ABC1D23", 625m, "Pending", DateTimeOffset.UtcNow, null, null, "public-token");
}
