using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Web.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class DeploymentConfigurationTests
{
    [Fact]
    public void Production_api_should_reject_missing_or_development_configuration()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Username=postgres;Password=postgres",
            ["IdentitySeed:Enabled"] = "true",
            ["App:PublicBaseUrl"] = "http://localhost:5123"
        });

        Should.Throw<InvalidOperationException>(() =>
            ApiDeploymentConfiguration.ValidateProduction(configuration, Environment("Production")));
    }

    [Fact]
    public void Production_api_should_accept_explicit_safe_deployment_configuration()
    {
        var configuration = SafeApiConfiguration();

        Should.NotThrow(() =>
            ApiDeploymentConfiguration.ValidateProduction(configuration, Environment("Production")));
    }

    [Fact]
    public void Production_web_should_require_non_local_api_keys_and_known_proxy()
    {
        var unsafeConfiguration = Configuration(new Dictionary<string, string?>
        {
            ["Api:BaseUrl"] = "http://localhost:5183"
        });
        Should.Throw<InvalidOperationException>(() =>
            WebDeploymentConfiguration.ValidateProduction(unsafeConfiguration, Environment("Production")));

        var safeConfiguration = Configuration(new Dictionary<string, string?>
        {
            ["Api:BaseUrl"] = "https://api.internal",
            ["DataProtection:KeysPath"] = "/persistent/keys",
            ["ReverseProxy:KnownProxies:0"] = "10.0.0.10"
        });
        Should.NotThrow(() =>
            WebDeploymentConfiguration.ValidateProduction(safeConfiguration, Environment("Production")));
    }

    [Fact]
    public void Development_should_remain_convenient_and_use_stable_data_protection_names()
    {
        var emptyConfiguration = Configuration(new Dictionary<string, string?>());
        Should.NotThrow(() => ApiDeploymentConfiguration.ValidateProduction(emptyConfiguration, Environment("Development")));
        Should.NotThrow(() => WebDeploymentConfiguration.ValidateProduction(emptyConfiguration, Environment("Development")));
        ApiDeploymentConfiguration.DataProtectionApplicationName.ShouldBe("SBGarage.Api");
        WebDeploymentConfiguration.DataProtectionApplicationName.ShouldBe("SBGarage.Web");
    }

    [Fact]
    public void Antiforgery_cookie_names_should_isolate_local_environments()
    {
        var development = Configuration(new Dictionary<string, string?>());
        var localProduction = Configuration(new Dictionary<string, string?>
        {
            ["Deployment:LocalProduction"] = "true"
        });
        var production = Configuration(new Dictionary<string, string?>());

        WebDeploymentConfiguration.GetAntiforgeryCookieName(development, Environment("Development"))
            .ShouldBe(WebDeploymentConfiguration.DevelopmentAntiforgeryCookieName);
        WebDeploymentConfiguration.GetAntiforgeryCookieName(localProduction, Environment("Production"))
            .ShouldBe(WebDeploymentConfiguration.LocalProductionAntiforgeryCookieName);
        WebDeploymentConfiguration.GetAntiforgeryCookieName(production, Environment("Production"))
            .ShouldBe(WebDeploymentConfiguration.ProductionAntiforgeryCookieName);
        WebDeploymentConfiguration.DevelopmentAntiforgeryCookieName
            .ShouldNotBe(WebDeploymentConfiguration.LocalProductionAntiforgeryCookieName);
    }

    [Fact]
    public void Production_api_should_allow_password_recovery_with_valid_email_provider()
    {
        var values = SafeApiValues();
        values["PasswordRecovery:Enabled"] = "true";

        Should.NotThrow(() =>
            ApiDeploymentConfiguration.ValidateProduction(Configuration(values), Environment("Production")));
    }

    [Fact]
    public void Production_api_should_reject_disabled_password_recovery()
    {
        var values = SafeApiValues();
        values["PasswordRecovery:Enabled"] = "false";

        Should.Throw<InvalidOperationException>(() =>
            ApiDeploymentConfiguration.ValidateProduction(Configuration(values), Environment("Production")));
    }

    [Fact]
    public void Production_api_should_reject_incomplete_smtp_configuration()
    {
        var values = SafeApiValues();
        values["Email:Password"] = null;
        Should.Throw<InvalidOperationException>(() =>
            ApiDeploymentConfiguration.ValidateProduction(Configuration(values), Environment("Production")));
    }

    [Fact]
    public void Explicit_local_production_should_allow_loopback_http_and_development_email_only()
    {
        var values = SafeApiValues();
        values["Deployment:LocalProduction"] = "true";
        values["App:PublicBaseUrl"] = "http://localhost:8080";
        values["Email:Provider"] = "Development";
        values["Email:Host"] = values["Email:Username"] = values["Email:Password"] = values["Email:FromAddress"] = null;

        Should.NotThrow(() => ApiDeploymentConfiguration.ValidateProduction(
            Configuration(values), Environment("Production")));

        values["Deployment:LocalProduction"] = "false";
        Should.Throw<InvalidOperationException>(() => ApiDeploymentConfiguration.ValidateProduction(
            Configuration(values), Environment("Production")));
    }

    [Fact]
    public void Local_production_should_not_accept_an_arbitrary_non_smtp_provider()
    {
        var values = SafeApiValues();
        values["Deployment:LocalProduction"] = "true";
        values["App:PublicBaseUrl"] = "http://localhost:8080";
        values["Email:Provider"] = "Disabled";

        Should.Throw<InvalidOperationException>(() => ApiDeploymentConfiguration.ValidateProduction(
            Configuration(values), Environment("Production")));
    }

    [Fact]
    public void Generic_exception_message_should_not_expose_technical_details()
    {
        ApiDeploymentConfiguration.UnexpectedErrorMessage.ShouldBe("Ocorreu um erro inesperado.");
        ApiDeploymentConfiguration.UnexpectedErrorMessage.ShouldNotContain("PostgreSQL");
        ApiDeploymentConfiguration.UnexpectedErrorMessage.ShouldNotContain("Exception");
    }

    private static IConfiguration SafeApiConfiguration() => Configuration(SafeApiValues());

    private static Dictionary<string, string?> SafeApiValues() => new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Host=db.internal;Username=app;Password=strong-secret",
        ["IdentitySeed:Enabled"] = "false",
        ["PasswordRecovery:Enabled"] = "true",
        ["App:PublicBaseUrl"] = "https://garage.example",
        ["Web:BaseUrl"] = "https://web.internal",
        ["Email:Provider"] = "Smtp",
        ["Email:Host"] = "smtp.example",
        ["Email:Port"] = "587",
        ["Email:Username"] = "mailer",
        ["Email:Password"] = "secret",
        ["Email:FromAddress"] = "no-reply@example.com",
        ["Email:TimeoutSeconds"] = "15",
        ["DataProtection:KeysPath"] = "/persistent/keys",
        ["ReverseProxy:KnownProxies:0"] = "10.0.0.10"
    };

    private static IConfiguration Configuration(IDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static IHostEnvironment Environment(string name) => new TestHostEnvironment { EnvironmentName = name };

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
