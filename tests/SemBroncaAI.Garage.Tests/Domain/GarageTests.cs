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

    [Fact]
    public void Should_Update_Settings_And_Preserve_Identity_State()
    {
        var garage = new GarageEntity("Oficina", "123", "1199", "a@b.com");
        var id = garage.Id; var createdAt = garage.CreatedAt; var active = garage.Active;

        garage.UpdateSettings("Oficina Nova", "456", "1188", "novo@oficina.com",
            "18500-000", "Rua Central", "123", "Sala 2", "Centro", "Boituva", "sp");

        garage.Id.ShouldBe(id); garage.CreatedAt.ShouldBe(createdAt); garage.Active.ShouldBe(active);
        garage.Name.ShouldBe("Oficina Nova"); garage.Street.ShouldBe("Rua Central");
        garage.City.ShouldBe("Boituva"); garage.State.ShouldBe("SP");
    }

    [Theory]
    [InlineData("", "123", "1199", "a@b.com")]
    [InlineData("Oficina", "", "1199", "a@b.com")]
    [InlineData("Oficina", "123", "", "a@b.com")]
    [InlineData("Oficina", "123", "1199", "")]
    public void Should_Reject_Required_Settings(string name, string document, string phone, string email)
    {
        Should.Throw<ArgumentException>(() => new GarageEntity(name, document, phone, email));
    }

    [Fact]
    public void Should_Reject_Address_That_Exceeds_Database_Limit()
    {
        var garage = new GarageEntity("Oficina", "123", "1199", "a@b.com");
        var exception = Should.Throw<ArgumentException>(() => garage.UpdateSettings(
            garage.Name, garage.Document, garage.Phone, garage.Email,
            null, null, null, null, null, new string('X', 101), "sp"));
        exception.Message.ShouldContain("100 caracteres");
    }

    [Fact]
    public void Should_Update_Valid_Branding_Without_Changing_Garage_Identity()
    {
        var garage = new GarageEntity("Oficina", "123", "1199", "a@b.com");
        var id = garage.Id; var createdAt = garage.CreatedAt; var active = garage.Active;
        garage.UpdateBranding($"{garage.Id:N}/logo.png", "#1a2b3c");
        garage.LogoStorageKey.ShouldBe($"{garage.Id:N}/logo.png"); garage.PrimaryColor.ShouldBe("#1A2B3C");
        garage.Id.ShouldBe(id); garage.CreatedAt.ShouldBe(createdAt); garage.Active.ShouldBe(active);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("#GG0000")]
    public void Should_Reject_Invalid_Primary_Color(string color)
    {
        var garage = new GarageEntity("Oficina", "123", "1199", "a@b.com");
        Should.Throw<ArgumentException>(() => garage.UpdateBranding(null, color));
    }
}
