using System.Security.Claims;
using SemBroncaAI.Garage.Application.Abstractions.Security;

namespace SemBroncaAI.Garage.Web.Services;

public static class AuthorizedLandingPage
{
    public static string For(ClaimsPrincipal user)
    {
        if (user.IsInRole("PlatformAdmin")) return "/platform-admin";
        if (Has(user, ApplicationPermissions.CreateServiceOrder)) return "/";
        if (Has(user, ApplicationPermissions.ViewServiceOrders)) return "/service-orders";
        if (Has(user, ApplicationPermissions.ViewCustomersVehicles)) return "/customers";
        if (Has(user, ApplicationPermissions.ViewEstimateValues)) return "/estimates";
        if (Has(user, ApplicationPermissions.ManageGarageSettings)) return "/settings";
        return "/access-denied";
    }

    public static string Resolve(ClaimsPrincipal user, string? requestedPath)
    {
        var path = NormalizeLocalPath(requestedPath);
        return path is not null && CanAccess(user, path) ? path : For(user);
    }

    private static bool CanAccess(ClaimsPrincipal user, string path)
    {
        if (path.Equals("/platform-admin", StringComparison.OrdinalIgnoreCase)) return user.IsInRole("PlatformAdmin");
        if (user.IsInRole("PlatformAdmin")) return false;
        if (path.Equals("/", StringComparison.OrdinalIgnoreCase)) return Has(user, ApplicationPermissions.CreateServiceOrder);
        if (path.StartsWith("/service-orders", StringComparison.OrdinalIgnoreCase)) return Has(user, ApplicationPermissions.ViewServiceOrders);
        if (path.StartsWith("/customers", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/vehicles", StringComparison.OrdinalIgnoreCase))
            return Has(user, ApplicationPermissions.ViewCustomersVehicles);
        if (path.StartsWith("/estimates", StringComparison.OrdinalIgnoreCase)) return Has(user, ApplicationPermissions.ViewEstimateValues);
        if (path.StartsWith("/settings", StringComparison.OrdinalIgnoreCase)) return Has(user, ApplicationPermissions.ManageGarageSettings);
        return false;
    }

    private static string? NormalizeLocalPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith('/') || value.StartsWith("//")) return null;
        return Uri.TryCreate(value, UriKind.Relative, out _) ? value : null;
    }

    private static bool Has(ClaimsPrincipal user, string permission) => user.HasClaim(ApplicationPermissions.ClaimType, permission);
}
