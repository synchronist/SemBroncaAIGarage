using SemBroncaAI.Garage.Web.Models;
using SemBroncaAI.Garage.Web.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class ServiceOrderEstimatePrintBuilderTests
{
    [Fact]
    public void Build_Should_Map_Client_Document_Without_Internal_Notes()
    {
        var document = ServiceOrderEstimatePrintBuilder.Build(Order(withEstimate: true), Garage());

        document.ShouldNotBeNull();
        document.Diagnosis.ShouldNotBeNull();
        document.Diagnosis.Description.ShouldBe("Falha identificada");
        typeof(EstimateDiagnosisPrintModel).GetProperty("InternalNotes").ShouldBeNull();
        document.Estimate.ServicesSubtotal.ShouldBe(200m);
        document.Estimate.PartsSubtotal.ShouldBe(100m);
        document.Estimate.Total.ShouldBe(300m);
        document.Garage.Name.ShouldBe("Oficina");
        document.Garage.City.ShouldBeNull();
        document.Garage.State.ShouldBeNull();
        document.Garage.PrimaryColor.ShouldBe(ServiceOrderPrintBuilder.DefaultPrimaryColor);
        typeof(EstimateDiagnosisPrintModel).GetProperty("InternalNotes").ShouldBeNull();
    }

    [Fact]
    public void Build_Should_Return_Null_When_Estimate_Does_Not_Exist()
    {
        ServiceOrderEstimatePrintBuilder.Build(Order(withEstimate: false), Garage()).ShouldBeNull();
    }

    [Fact]
    public void Build_Should_Omit_Logo_When_Garage_Has_No_Logo()
    {
        var document = ServiceOrderEstimatePrintBuilder.Build(Order(withEstimate: true), Garage(), "/auth/garage-logo");
        document.ShouldNotBeNull();
        document.Garage.LogoUrl.ShouldBeNull();
    }

    [Fact]
    public void Build_Should_Carry_Partial_Approval_Evidence_To_The_Document()
    {
        var order = Order(withEstimate: true);
        var selectedId = order.Estimate!.Items.First().Id;
        order = order with
        {
            Estimate = order.Estimate with
            {
                Items =
                [
                    order.Estimate.Items.First() with { AuthorizationStatus = "CustomerAuthorized" },
                    order.Estimate.Items.Last() with { AuthorizationStatus = "CustomerNotAuthorized" }
                ]
            }
        };
        order = order with
        {
            Approval = new ServiceOrderApprovalModel("PartiallyApproved", DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(6), DateTimeOffset.UtcNow, "Cliente da Silva", "Sem as peças", null)
            {
                ApprovedTotal = 200m,
                CustomerDocumentMasked = "***.982.247-**",
                ApprovedItemIds = [selectedId]
            }
        };

        var document = ServiceOrderEstimatePrintBuilder.Build(order, Garage());

        document!.Approval!.Status.ShouldBe("PartiallyApproved");
        document.Approval.ApprovedItemIds.ShouldContain(selectedId);
        document.Estimate.Items.First().AuthorizationStatus.ShouldBe("CustomerAuthorized");
        document.Estimate.Items.Last().AuthorizationStatus.ShouldBe("CustomerNotAuthorized");
    }

    [Theory]
    [InlineData("horizontal")]
    [InlineData("square")]
    [InlineData("vertical")]
    public void Valid_logo_shapes_should_use_same_authenticated_document_route(string shape)
    {
        var garage = Garage(); garage.LogoStorageKey = $"garage/logo-{shape}.webp";
        var document = ServiceOrderEstimatePrintBuilder.Build(Order(withEstimate: true), garage, "/auth/garage-logo");
        document.ShouldNotBeNull();
        document.Garage.LogoUrl.ShouldBe("/auth/garage-logo");
    }

    private static GarageSettingsModel Garage() => new()
    {
        Id = Guid.CreateVersion7(), Name = "Oficina", Document = "123", Phone = "1199", Email = "a@b.com"
    };

    private static ServiceOrderDetailsModel Order(bool withEstimate)
    {
        var estimate = withEstimate
            ? new ServiceOrderEstimateModel(Guid.CreateVersion7(), 200m, 100m, 300m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                [new(Guid.CreateVersion7(), "Serviço", "Service", 1, 200m, 200m), new(Guid.CreateVersion7(), "Peça", "Part", 1, 100m, 100m)])
            : null;
        return new(Guid.CreateVersion7(), Guid.CreateVersion7(), 7, "WaitingApproval", "Relato", 50000, DateTimeOffset.UtcNow,
            new(Guid.CreateVersion7(), "Cliente", "456", "1188", "c@d.com"),
            new(Guid.CreateVersion7(), "ABC1234", "Fiat", "Uno", "Way", 2020, "Prata", "Flex", 50000), [],
            new(Guid.CreateVersion7(), "Falha identificada", "NÃO MOSTRAR", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), estimate);
    }
}
