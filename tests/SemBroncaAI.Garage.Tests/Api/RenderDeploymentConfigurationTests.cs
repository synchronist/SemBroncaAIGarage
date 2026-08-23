using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Web.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class RenderDeploymentConfigurationTests
{
    [Fact]
    public void Production_should_accept_explicit_render_proxy_trust_on_render()
    {
        var api = Configuration(SafeApiValues());
        var web = Configuration(new Dictionary<string, string?>
        {
            ["Api:BaseUrl"] = "https://sembronca-garage-api.onrender.com/",
            ["DataProtection:KeysPath"] = "/tmp/sbgarage/data-protection",
            ["ReverseProxy:TrustRenderProxy"] = "true",
            ["RENDER"] = "true"
        });

        Should.NotThrow(() => ApiDeploymentConfiguration.ValidateProduction(api, Production()));
        Should.NotThrow(() => WebDeploymentConfiguration.ValidateProduction(web, Production()));
    }

    [Fact]
    public void Production_should_reject_render_proxy_trust_outside_render()
    {
        var values = SafeApiValues();
        values["RENDER"] = "false";

        Should.Throw<InvalidOperationException>(() =>
            ApiDeploymentConfiguration.ValidateProduction(Configuration(values), Production()));
    }

    private static Dictionary<string, string?> SafeApiValues() => new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Host=db.internal;Username=app;Password=strong-secret",
        ["IdentitySeed:Enabled"] = "false",
        ["PasswordRecovery:Enabled"] = "true",
        ["App:PublicBaseUrl"] = "https://sembronca-garage.onrender.com",
        ["Web:BaseUrl"] = "https://sembronca-garage.onrender.com/",
        ["Email:Provider"] = "Smtp",
        ["Email:Host"] = "smtp.example",
        ["Email:Port"] = "2525",
        ["Email:Username"] = "mailer",
        ["Email:Password"] = "secret",
        ["Email:FromAddress"] = "no-reply@example.com",
        ["Email:TimeoutSeconds"] = "15",
        ["DataProtection:KeysPath"] = "/tmp/sbgarage/data-protection",
        ["ReverseProxy:TrustRenderProxy"] = "true",
        ["RENDER"] = "true"
    };

    private static IConfiguration Configuration(IDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static IHostEnvironment Production() => new TestHostEnvironment();

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
