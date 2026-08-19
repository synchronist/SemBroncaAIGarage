using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

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
        var dataProtection = builder.Services.AddDataProtection().SetApplicationName(DataProtectionApplicationName);
        if (builder.Environment.IsProduction())
        {
            var path = builder.Configuration["DataProtection:KeysPath"]!;
            Directory.CreateDirectory(path);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(path));
        }

        var proxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            foreach (var proxy in proxies) options.KnownProxies.Add(IPAddress.Parse(proxy));
        });
    }

    public static void ValidateProduction(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction()) return;
        var apiBaseUrl = configuration["Api:BaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl) || apiBaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Configure Api:BaseUrl de Production.");
        if (string.IsNullOrWhiteSpace(configuration["DataProtection:KeysPath"]))
            throw new InvalidOperationException("Configure DataProtection:KeysPath em Production.");
        if (!configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>()?.Any() ?? true)
            throw new InvalidOperationException("Configure ReverseProxy:KnownProxies em Production.");
    }
}
