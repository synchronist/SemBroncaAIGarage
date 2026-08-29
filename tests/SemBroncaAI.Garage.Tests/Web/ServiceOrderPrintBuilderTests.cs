using SemBroncaAI.Garage.Web.Models;
using SemBroncaAI.Garage.Web.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class ServiceOrderPrintBuilderTests
{
    [Fact]
    public void Build_Should_Not_Expose_Internal_Notes_And_Should_Map_Totals_And_Garage()
    {
        var document = ServiceOrderPrintBuilder.Build(Order(withDiagnosis: true, withEstimate: true), Garage());

        document.Diagnosis.ShouldNotBeNull();
        document.Diagnosis.Description.ShouldBe("Trocar bieleta");
        typeof(DiagnosisPrintModel).GetProperty("InternalNotes").ShouldBeNull();
        document.Estimate.ShouldNotBeNull();
        document.Estimate.ServicesSubtotal.ShouldBe(150m);
        document.Estimate.PartsSubtotal.ShouldBe(80m);
        document.Estimate.Total.ShouldBe(230m);
        document.Estimate.ApprovalStatus.ShouldBeNull();
        document.Estimate.ApprovedTotal.ShouldBeNull();
        document.Estimate.DigitalApprovalWaived.ShouldBeFalse();
        document.Garage.Name.ShouldBe("Oficina do João");
        document.Garage.City.ShouldBe("Boituva");
        document.Garage.State.ShouldBe("SP");
        document.Garage.PrimaryColor.ShouldBe(ServiceOrderPrintBuilder.DefaultPrimaryColor);
        document.Garage.LogoUrl.ShouldBeNull();
    }

    [Fact]
    public void Build_Should_Handle_Missing_Diagnosis_And_Estimate()
    {
        var document = ServiceOrderPrintBuilder.Build(Order(false, false), Garage());
        document.Diagnosis.ShouldBeNull();
        document.Estimate.ShouldBeNull();
    }

    [Fact]
    public void Build_Should_Map_Configured_Logo_And_Color()
    {
        var garage = Garage(); garage.LogoStorageKey = $"{garage.Id:N}/logo.png"; garage.PrimaryColor = "#123ABC";
        var document = ServiceOrderPrintBuilder.Build(Order(false, false), garage, "https://api/logo");
        document.Garage.LogoUrl.ShouldBe("https://api/logo");
        document.Garage.PrimaryColor.ShouldBe("#123ABC");
    }

    [Theory]
    [InlineData("Approved", 230, false)]
    [InlineData("PartiallyApproved", 150, false)]
    public void Build_should_distinguish_original_and_authorized_totals(string status, decimal approvedTotal, bool waiver)
    {
        var order = Order(true, true) with
        {
            Approval = new ServiceOrderApprovalModel(status, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow, "Cliente", null, null) { ApprovedTotal = approvedTotal },
            DigitalApprovalWaivedAt = waiver ? DateTimeOffset.UtcNow : null
        };
        var estimate = ServiceOrderPrintBuilder.Build(order, Garage()).Estimate!;
        estimate.Total.ShouldBe(230m);
        estimate.ApprovedTotal.ShouldBe(approvedTotal);
        estimate.ApprovalStatus.ShouldBe(status);
    }

    [Fact]
    public void Build_should_keep_waiver_separate_from_customer_approval()
    {
        var order = Order(true, true) with { DigitalApprovalWaivedAt = DateTimeOffset.UtcNow };
        var estimate = ServiceOrderPrintBuilder.Build(order, Garage()).Estimate!;
        estimate.DigitalApprovalWaived.ShouldBeTrue();
        estimate.ApprovalStatus.ShouldBeNull();
        estimate.ApprovedTotal.ShouldBeNull();
    }

    private static GarageSettingsModel Garage() => new()
    {
        Id = Guid.CreateVersion7(), Name = "Oficina do João", Document = "123", Phone = "1199",
        Email = "a@b.com", Street = "Rua A", Number = "10", City = "Boituva", State = "SP"
    };

    private static ServiceOrderDetailsModel Order(bool withDiagnosis, bool withEstimate)
    {
        var diagnosis = withDiagnosis
            ? new ServiceOrderDiagnosisModel(Guid.CreateVersion7(), "Trocar bieleta", "SEGREDO INTERNO", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            : null;
        var estimate = withEstimate
            ? new ServiceOrderEstimateModel(Guid.CreateVersion7(), 150m, 80m, 230m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                [new(Guid.CreateVersion7(), "Bieleta", "Part", 1, 80m, 80m), new(Guid.CreateVersion7(), "Troca", "Service", 1, 150m, 150m)])
            : null;
        return new(Guid.CreateVersion7(), Guid.CreateVersion7(), 1, "Diagnosis", "Ruído na suspensão", 36250,
            DateTimeOffset.UtcNow, new(Guid.CreateVersion7(), "Cliente", "456", "1188", "c@d.com"),
            new(Guid.CreateVersion7(), "ABC1234", "Fiat", "Uno", "Way", 2020, "Prata", "Flex", 40000),
            [], diagnosis, estimate);
    }
}
