using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace SemBroncaAI.Garage.Api.Services;

public static class ApiDeploymentConfiguration
{
    public const string DataProtectionApplicationName = "SBGarage.Api";
    public const string UnexpectedErrorMessage = "Ocorreu um erro inesperado.";

    public static void Configure(WebApplicationBuilder builder)
    {
        ValidateProduction(builder.Configuration, builder.Environment);
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
        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection) || connection.Contains("Password=postgres", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection de forma segura em Production.");
        if (configuration.GetValue<bool>("IdentitySeed:Enabled"))
            throw new InvalidOperationException("IdentitySeed não pode ser habilitado em Production.");
        if (configuration.GetValue<bool>("PasswordRecovery:Enabled"))
            throw new InvalidOperationException("PasswordRecovery não pode ser habilitado em Production sem provider de e-mail.");
        var webBaseUrl = configuration["Web:BaseUrl"];
        if (string.IsNullOrWhiteSpace(webBaseUrl) || webBaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Configure Web:BaseUrl de Production.");
        if (string.IsNullOrWhiteSpace(configuration["DataProtection:KeysPath"]))
            throw new InvalidOperationException("Configure DataProtection:KeysPath em Production.");
        if (!configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>()?.Any() ?? true)
            throw new InvalidOperationException("Configure ReverseProxy:KnownProxies em Production.");
    }
}
