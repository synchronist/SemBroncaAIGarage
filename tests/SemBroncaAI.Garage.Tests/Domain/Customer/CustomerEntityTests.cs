using SemBroncaAI.Garage.Domain.Entities.Customer;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Domain.Customer;

public sealed class CustomerEntityTests
{
    [Fact]
    public void Should_Create_Valid_Customer()
    {
        var garageId = Guid.CreateVersion7();
        var customer = new CustomerEntity(garageId, "Maria Silva", "12345678900", "11999999999", "maria@email.com");
        customer.GarageId.ShouldBe(garageId);
        customer.Name.ShouldBe("Maria Silva");
        customer.Active.ShouldBeTrue();
    }

    [Fact]
    public void Should_Update_Customer()
    {
        var customer = CreateCustomer();
        customer.Update("Maria Souza", "98765432100", "11888888888", "souza@email.com");
        customer.Name.ShouldBe("Maria Souza");
        customer.Document.ShouldBe("98765432100");
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
    [InlineData("Maria", "", "11999", "maria@email.com")]
    [InlineData("Maria", "123", "", "maria@email.com")]
    public void Should_Reject_Required_Fields(string name, string document, string phone, string email)
    {
        Should.Throw<ArgumentException>(() => new CustomerEntity(Guid.CreateVersion7(), name, document, phone, email));
    }

    [Fact]
    public void Should_Reject_Invalid_Email()
    {
        Should.Throw<ArgumentException>(() => new CustomerEntity(Guid.CreateVersion7(), "Maria", "123", "11999", "email-invalido"));
    }

    private static CustomerEntity CreateCustomer() =>
        new(Guid.CreateVersion7(), "Maria Silva", "12345678900", "11999999999", "maria@email.com");
}
