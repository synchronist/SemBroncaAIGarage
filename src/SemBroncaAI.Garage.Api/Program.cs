using System.Text.Json.Serialization;
using SemBroncaAI.Garage.Infrastructure;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Application.Abstractions.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDataProtection();
builder.Services.AddScoped<IDocumentPdfGenerator, PlaywrightDocumentPdfGenerator>();
builder.Services.AddSingleton<IApprovalTokenService, ApprovalTokenService>();
builder.Services.AddRateLimiter(ApprovalRateLimiting.Configure);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

var app = builder.Build();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
