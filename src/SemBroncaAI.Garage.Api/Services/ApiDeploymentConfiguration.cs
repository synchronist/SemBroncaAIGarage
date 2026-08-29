using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using SemBroncaAI.Garage.DataProtection;

namespace SemBroncaAI.Garage.Api.Services;

public static class ApiDeploymentConfiguration
{
    public const string DataProtectionApplicationName = "SBGarage.Api";
    public const string UnexpectedErrorMessage = "Ocorreu um erro inesperado.";

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
        var localProduction = configuration.GetValue<bool>("Deployment:LocalProduction");
        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection) || connection.Contains("Password=postgres", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection de forma segura em Production.");
        if (configuration.GetValue<bool>("IdentitySeed:Enabled"))
            throw new InvalidOperationException("IdentitySeed não pode ser habilitado em Production.");
        var publicBaseUrl = configuration["App:PublicBaseUrl"];
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var publicUri) ||
            (!localProduction && (publicUri.Scheme != Uri.UriSchemeHttps || publicUri.IsLoopback)) ||
            (localProduction && !(publicUri.IsLoopback && publicUri.Scheme == Uri.UriSchemeHttp)))
            throw new InvalidOperationException("Configure App:PublicBaseUrl com uma URL HTTPS pública em Production.");
        if (localProduction)
        {
            if (!string.Equals(configuration["Email:Provider"], "Development", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("LocalProduction sem SMTP deve usar explicitamente Email:Provider=Development.");
        }
        else
        {
            ValidateEmail(configuration);
            if (!configuration.GetValue<bool>("PasswordRecovery:Enabled"))
                throw new InvalidOperationException("PasswordRecovery deve estar habilitado em Production.");
        }
        var webBaseUrl = configuration["Web:BaseUrl"];
        if (string.IsNullOrWhiteSpace(webBaseUrl) || webBaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Configure Web:BaseUrl de Production para geração de documentos.");
        DurableDataProtectionConfiguration.ValidateProduction(configuration, environment);
        ValidateReverseProxy(configuration);
    }

    private static void ValidateReverseProxy(IConfiguration configuration)
    {
        var trustRenderProxy = configuration.GetValue<bool>("ReverseProxy:TrustRenderProxy");
        var runningOnRender = string.Equals(configuration["RENDER"], "true", StringComparison.OrdinalIgnoreCase);
        if (trustRenderProxy && !runningOnRender)
            throw new InvalidOperationException("ReverseProxy:TrustRenderProxy só pode ser habilitado no Render.");
        if (!trustRenderProxy && (!configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>()?.Any() ?? true))
            throw new InvalidOperationException("Configure ReverseProxy:KnownProxies em Production.");
    }

    private static void ValidateEmail(IConfiguration configuration)
    {
        if (!string.Equals(configuration["Email:Provider"], "Smtp", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Configure Email:Provider como Smtp em Production.");
        if (string.IsNullOrWhiteSpace(configuration["Email:Host"]) ||
            configuration.GetValue<int>("Email:Port") is < 1 or > 65535 ||
            string.IsNullOrWhiteSpace(configuration["Email:Username"]) ||
            string.IsNullOrWhiteSpace(configuration["Email:Password"]) ||
            !System.Net.Mail.MailAddress.TryCreate(configuration["Email:FromAddress"], out _) ||
            configuration.GetValue<int>("Email:TimeoutSeconds") is < 1 or > 120)
            throw new InvalidOperationException("Configure o transporte SMTP de Production de forma completa e válida.");
    }
}
