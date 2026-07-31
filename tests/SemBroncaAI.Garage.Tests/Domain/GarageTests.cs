using FluentAssertions;
using GarageEntity = SemBroncaAI.Garage.Domain.Entities.Garage;

namespace SemBroncaAI.Garage.Tests.Domain;

public class GarageTests
{
    [Fact]
    public void Should_Create_Garage_When_Name_Is_Valid()
    {
        var garage = new GarageEntity(
            "Oficina do João",
            "12345678000199",
            "11999999999",
            "contato@oficina.com");

        garage.Id.Should().NotBeEmpty();
        garage.Name.Should().Be("Oficina do João");
        garage.Document.Should().Be("12345678000199");
        garage.Phone.Should().Be("11999999999");
        garage.Email.Should().Be("contato@oficina.com");
        garage.Active.Should().BeTrue();
        garage.CreatedAt.Should().BeCloseTo(
            DateTime.UtcNow,
            TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Should_Throw_When_Name_Is_Empty(string invalidName)
    {
        var action = () => new GarageEntity(
            invalidName,
            "12345678000199",
            "11999999999",
            "contato@oficina.com");

        action.Should()
            .Throw<ArgumentException>()
            .WithMessage("*nome da oficina é obrigatório*")
            .And.ParamName.Should()
            .Be("name");
    }
}