namespace SemBroncaAI.Garage.Application.Abstractions.Security;

public static class RolePermissionDefaults
{
    public const string Owner = "Owner";
    public const string Receptionist = "Receptionist";
    public const string Mechanic = "Mechanic";

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Defaults =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [Owner] = ApplicationPermissions.All.ToHashSet(StringComparer.Ordinal),
            [Receptionist] = new HashSet<string>(StringComparer.Ordinal)
            {
                ApplicationPermissions.ViewCustomersVehicles,
                ApplicationPermissions.ManageCustomersVehicles,
                ApplicationPermissions.CreateServiceOrder,
                ApplicationPermissions.ViewServiceOrders,
                ApplicationPermissions.CancelServiceOrder,
                ApplicationPermissions.DeliverServiceOrder,
                ApplicationPermissions.ArchiveServiceOrder,
                ApplicationPermissions.ViewDiagnosis,
                ApplicationPermissions.ViewEstimateValues,
                ApplicationPermissions.ManageEstimates,
                ApplicationPermissions.SendEstimateForApproval
            },
            [Mechanic] = new HashSet<string>(StringComparer.Ordinal)
            {
                ApplicationPermissions.ViewServiceOrders,
                ApplicationPermissions.ViewDiagnosis,
                ApplicationPermissions.ManageDiagnosis,
                ApplicationPermissions.StartService,
                ApplicationPermissions.ChangeServiceExecutionStatus,
                ApplicationPermissions.FinishService
            }
        };

    public static IReadOnlySet<string> ForRoles(IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in roles)
            if (Defaults.TryGetValue(role, out var defaults)) permissions.UnionWith(defaults);
        return permissions;
    }
}
