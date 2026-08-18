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
    public void Production_api_should_allow_password_recovery_with_valid_email_provider()
    {
        var values = SafeApiValues();
        values["PasswordRecovery:Enabled"] = "true";

        Should.NotThrow(() =>
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
        ["PasswordRecovery:Enabled"] = "false",
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
