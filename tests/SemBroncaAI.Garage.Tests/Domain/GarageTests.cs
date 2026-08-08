using Shouldly;
using GarageEntity =
    SemBroncaAI.Garage.Domain.Entities.Garage.GarageEntity;

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

        garage.Id.ShouldNotBe(Guid.Empty);
        garage.Name.ShouldBe("Oficina do João");
        garage.Document.ShouldBe("12345678000199");
        garage.Phone.ShouldBe("11999999999");
        garage.Email.ShouldBe("contato@oficina.com");
        garage.Active.ShouldBeTrue();

        garage.CreatedAt.ShouldBe(
            DateTime.UtcNow,
            TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Should_Throw_When_Name_Is_Empty(string invalidName)
    {
        var exception = Should.Throw<ArgumentException>(() =>
            new GarageEntity(
                invalidName,
                "12345678000199",
                "11999999999",
                "contato@oficina.com"));

        exception.Message.ShouldContain(
            "nome da oficina é obrigatório");

        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    public void Should_Deactivate_Garage()
    {
        var garage = new GarageEntity(
            "Oficina do João",
            "12345678000199",
            "11999999999",
            "contato@oficina.com");

        garage.Deactivate();

        garage.Active.ShouldBeFalse();
    }

    [Fact]
    public void Should_Activate_Garage()
    {
        var garage = new GarageEntity(
            "Oficina do João",
            "12345678000199",
            "11999999999",
            "contato@oficina.com");

        garage.Deactivate();
        garage.Activate();

        garage.Active.ShouldBeTrue();
    }

    [Fact]
    public void Should_Change_Contact_Information()
    {
        var garage = new GarageEntity(
            "Oficina do João",
            "12345678000199",
            "11999999999",
            "contato@oficina.com");

        garage.ChangeContactInformation(
            "11888888888",
            "novo@oficina.com");

        garage.Phone.ShouldBe("11888888888");
        garage.Email.ShouldBe("novo@oficina.com");
    }
}