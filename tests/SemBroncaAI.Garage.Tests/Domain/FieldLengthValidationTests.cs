using SemBroncaAI.Garage.Domain.Common;
using SemBroncaAI.Garage.Domain.Entities.Customer;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Domain;

public sealed class FieldLengthValidationTests
{
    [Fact]
    public void Customer_should_accept_limits_and_reject_value_above_limit()
    {
        Should.NotThrow(() => new CustomerEntity(Guid.NewGuid(), new string('N', FieldLengthLimits.PersonName),
            "52998224725", "11999999999",
            new string('a', FieldLengthLimits.Email - 6) + "@x.com"));
        Should.Throw<ArgumentException>(() => new CustomerEntity(Guid.NewGuid(),
            new string('N', FieldLengthLimits.PersonName + 1), "doc", "phone", "a@b.co"));
    }

    [Fact]
    public void Vehicle_service_order_diagnosis_and_estimate_should_reject_oversized_strings()
    {
        Should.Throw<ArgumentException>(() => new VehicleEntity(Guid.NewGuid(), Guid.NewGuid(), "ABC1D23",
            new string('B', FieldLengthLimits.VehicleBrand + 1), "Model", "", 2025, "", "", 0));
        Should.Throw<ArgumentException>(() => new ServiceOrderEntity(Guid.NewGuid(), Guid.NewGuid(), 1,
            new string('R', FieldLengthLimits.CustomerComplaint + 1), 0));

        var order = new ServiceOrderEntity(Guid.NewGuid(), Guid.NewGuid(), 1, "Relato", 0);
        order.StartDiagnosis();
        Should.Throw<ArgumentException>(() => order.SaveDiagnosis(new string('D', FieldLengthLimits.DiagnosisText + 1)));
        order.SaveDiagnosis("Diagnóstico");
        Should.Throw<ArgumentException>(() => order.SaveEstimate([
            new ServiceOrderEstimateItemData(new string('I', FieldLengthLimits.EstimateItemDescription + 1), EstimateItemType.Service, 1, 1)]));
    }
}
