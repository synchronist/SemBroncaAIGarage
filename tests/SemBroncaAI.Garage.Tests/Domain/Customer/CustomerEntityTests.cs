using SemBroncaAI.Garage.Domain.Common;
using SemBroncaAI.Garage.Domain.Entities.Customer;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Domain.Customer;

public sealed class CustomerEntityTests
{
    [Theory]
    [InlineData("telefone")]
    [InlineData("1199")]
    [InlineData("119999999999")]
    public void Customer_should_reject_invalid_phone_server_side(string phone)
    {
        Should.Throw<ArgumentException>(() => new CustomerEntity(Guid.NewGuid(), "Cliente", "", phone, "cliente@test.local"));
    }
    [Fact]
    public void Should_Create_Valid_Customer()
    {
        var garageId = Guid.CreateVersion7();
        var customer = new CustomerEntity(garageId, "Maria Silva", "529.982.247-25", "11999999999", "maria@email.com");
        customer.GarageId.ShouldBe(garageId);
        customer.Name.ShouldBe("Maria Silva");
        customer.Active.ShouldBeTrue();
    }

    [Fact]
    public void Should_Update_Customer()
    {
        var customer = CreateCustomer();
        customer.Update("Maria Souza", "11.222.333/0001-81", "11888888888", "souza@email.com");
        customer.Name.ShouldBe("Maria Souza");
        customer.Document.ShouldBe("11222333000181");
        customer.Phone.ShouldBe("11888888888");
        customer.Email.ShouldBe("souza@email.com");
    }

    [Fact]
    public void Should_Reject_Empty_Garage_Id()
    {
        Should.Throw<ArgumentException>(() => new CustomerEntity(Guid.Empty, "Maria", "123", "11999", "maria@email.com"));
    }

    [Theory]
    [InlineData("", "123", "11999", "maria@email.com")]
    [InlineData("Maria", "123", "", "maria@email.com")]
    public void Should_Reject_Required_Fields(string name, string document, string phone, string email)
    {
        Should.Throw<ArgumentException>(() => new CustomerEntity(Guid.CreateVersion7(), name, document, phone, email));
    }

    [Fact]
    public void Should_Accept_Empty_Optional_Document() =>
        Should.NotThrow(() => new CustomerEntity(Guid.CreateVersion7(), "Maria", "", "11999999999", "maria@email.com"));

    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    public void Should_Validate_And_Normalize_Brazilian_Document(string value, string expected)
    {
        var customer = new CustomerEntity(Guid.CreateVersion7(), "Maria", value, "11999999999", "maria@email.com");
        customer.Document.ShouldBe(expected);
    }

    [Fact]
    public void Should_Reject_Invalid_Brazilian_Document() =>
        Should.Throw<ArgumentException>(() => new CustomerEntity(Guid.CreateVersion7(), "Maria", "12345678900", "11999999999", "maria@email.com"));

    [Theory]
    [InlineData("CPF 529.982.247-25")]
    [InlineData("529A982B247C25")]
    public void Should_Reject_Alphabetic_Or_Alphanumeric_Document(string document) =>
        Should.Throw<ArgumentException>(() => new CustomerEntity(Guid.CreateVersion7(), "Maria", document, "11999999999", "maria@email.com"));

    [Theory]
    [InlineData("52998224725", "529.982.247-25")]
    [InlineData("11222333000181", "11.222.333/0001-81")]
    public void Should_Format_Brazilian_Document(string document, string expected) =>
        BrazilianDocument.Format(document).ShouldBe(expected);

    [Fact]
    public void Should_Reject_Invalid_Email()
    {
        Should.Throw<ArgumentException>(() => new CustomerEntity(Guid.CreateVersion7(), "Maria", "123", "11999", "email-invalido"));
    }

    private static CustomerEntity CreateCustomer() =>
        new(Guid.CreateVersion7(), "Maria Silva", "52998224725", "11999999999", "maria@email.com");
}
