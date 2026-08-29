using SemBroncaAI.Garage.Web.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class BrazilianInputFormatterTests
{
    [Theory]
    [InlineData("abc52998224725xyz", "529.982.247-25")]
    [InlineData("12.345.678/0001-95extra", "12.345.678/0001-95")]
    public void Document_should_filter_limit_and_mask_live(string input, string expected) =>
        BrazilianInputFormatter.Document(input).ShouldBe(expected);

    [Theory]
    [InlineData("abc11987654321xyz", "(11) 98765-4321")]
    [InlineData("1132654321", "(11) 3265-4321")]
    public void Phone_should_filter_limit_and_mask_live(string input, string expected) =>
        BrazilianInputFormatter.Phone(input).ShouldBe(expected);

    [Theory]
    [InlineData("abc1234", "ABC-1234")]
    [InlineData("abc1d23", "ABC1D23")]
    [InlineData("a!b@c#1$d%2&3extra", "ABC1D23")]
    public void Plate_should_support_old_and_mercosul_patterns(string input, string expected) =>
        BrazilianInputFormatter.Plate(input).ShouldBe(expected);

    [Fact]
    public void Public_approval_cpf_should_never_exceed_eleven_digits() =>
        BrazilianInputFormatter.Cpf("529982247251234").ShouldBe("529.982.247-25");
}
