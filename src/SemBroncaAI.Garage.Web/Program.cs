using MudBlazor.Services;
using SemBroncaAI.Garage.Web.Components;
using SemBroncaAI.Garage.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddHttpClient<LookupService>((serviceProvider, client) =>
{
    var configuration =
        serviceProvider.GetRequiredService<IConfiguration>();

    var baseUrl =
        configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException(
            "A URL da API não foi configurada.");

    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddHttpClient<ServiceOrderService>(
    (serviceProvider, client) =>
    {
        var configuration =
            serviceProvider.GetRequiredService<IConfiguration>();

        var baseUrl =
            configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException(
                "A URL da API não foi configurada.");

        client.BaseAddress = new Uri(baseUrl);
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();