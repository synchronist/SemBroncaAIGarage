namespace SemBroncaAI.Garage.Application.Abstractions.Security;

public static class ApplicationPermissions
{
    public const string ClaimType = "permission";
    public const string ViewCustomersVehicles = "customers-vehicles.view";
    public const string ManageCustomersVehicles = "customers-vehicles.manage";
    public const string CreateServiceOrder = "service-orders.create";
    public const string ViewServiceOrders = "service-orders.view";
    public const string CancelServiceOrder = "service-orders.cancel";
    public const string DeliverServiceOrder = "service-orders.deliver";
    public const string ArchiveServiceOrder = "service-orders.archive";
    public const string ViewDiagnosis = "diagnosis.view";
    public const string ManageDiagnosis = "diagnosis.manage";
    public const string ViewEstimateValues = "estimates.values.view";
    public const string ManageEstimates = "estimates.manage";
    public const string SendEstimateForApproval = "estimates.send-for-approval";
    public const string StartService = "service-execution.start";
    public const string ChangeServiceExecutionStatus = "service-execution.status.manage";
    public const string FinishService = "service-execution.finish";
    public const string ManageGarageSettings = "garage-settings.manage";
    public const string ManageTeam = "team.manage";

    public static IReadOnlyCollection<string> All { get; } =
    [
        ViewCustomersVehicles, ManageCustomersVehicles, CreateServiceOrder, ViewServiceOrders,
        CancelServiceOrder, DeliverServiceOrder, ArchiveServiceOrder, ViewDiagnosis, ManageDiagnosis,
        ViewEstimateValues, ManageEstimates, SendEstimateForApproval, StartService,
        ChangeServiceExecutionStatus, FinishService, ManageGarageSettings, ManageTeam
    ];
}
