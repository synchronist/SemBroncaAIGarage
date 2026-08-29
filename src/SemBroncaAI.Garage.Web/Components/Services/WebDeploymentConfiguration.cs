using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using SemBroncaAI.Garage.DataProtection;

namespace SemBroncaAI.Garage.Web.Services;

public static class WebDeploymentConfiguration
{
    public const string DataProtectionApplicationName = "SBGarage.Web";
    public const string DevelopmentAntiforgeryCookieName = "SBGarage.Development.Antiforgery";
    public const string LocalProductionAntiforgeryCookieName = "SBGarage.LocalProduction.Antiforgery";
    public const string ProductionAntiforgeryCookieName = "__Host-SBGarage.Antiforgery";

    public static string GetAntiforgeryCookieName(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment()) return DevelopmentAntiforgeryCookieName;
        return configuration.GetValue<bool>("Deployment:LocalProduction")
            ? LocalProductionAntiforgeryCookieName
            : ProductionAntiforgeryCookieName;
    }

    public static void Configure(WebApplicationBuilder builder)
    {
        ValidateProduction(builder.Configuration, builder.Environment);
        if (builder.Environment.IsProduction())
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
        }
        DurableDataProtectionConfiguration.Configure(
            builder.Services,
            builder.Configuration,
            builder.Environment,
            DataProtectionApplicationName);

        var proxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
        var trustRenderProxy = builder.Configuration.GetValue<bool>("ReverseProxy:TrustRenderProxy");
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            if (!trustRenderProxy)
            {
                foreach (var proxy in proxies) options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        });
    }

    public static void ValidateProduction(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction()) return;
        var apiBaseUrl = configuration["Api:BaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl) || apiBaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Configure Api:BaseUrl de Production.");
        DurableDataProtectionConfiguration.ValidateProduction(configuration, environment);
        var trustRenderProxy = configuration.GetValue<bool>("ReverseProxy:TrustRenderProxy");
        var runningOnRender = string.Equals(configuration["RENDER"], "true", StringComparison.OrdinalIgnoreCase);
        if (trustRenderProxy && !runningOnRender)
            throw new InvalidOperationException("ReverseProxy:TrustRenderProxy só pode ser habilitado no Render.");
        if (!trustRenderProxy && (!configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>()?.Any() ?? true))
            throw new InvalidOperationException("Configure ReverseProxy:KnownProxies em Production.");
    }
}
