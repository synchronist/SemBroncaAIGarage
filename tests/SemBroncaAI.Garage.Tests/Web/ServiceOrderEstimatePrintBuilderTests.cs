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
    }

    [Fact]
    public void Build_Should_Return_Null_When_Estimate_Does_Not_Exist()
    {
        ServiceOrderEstimatePrintBuilder.Build(Order(withEstimate: false), Garage()).ShouldBeNull();
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
