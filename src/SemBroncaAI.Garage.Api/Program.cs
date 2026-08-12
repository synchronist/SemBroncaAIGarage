using System.Text.Json.Serialization;
using SemBroncaAI.Garage.Infrastructure;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDataProtection();
builder.Services.AddScoped<IDocumentPdfGenerator, PlaywrightDocumentPdfGenerator>();
builder.Services.AddSingleton<IApprovalTokenService, ApprovalTokenService>();
builder.Services.AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme, options =>
    {
        options.BearerTokenExpiration = TimeSpan.FromDays(7);
        options.RefreshTokenExpiration = TimeSpan.FromDays(7);
    });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("ActiveUser", policy =>
        policy.RequireAuthenticatedUser().AddRequirements(new ActiveUserRequirement())));
builder.Services.AddScoped<IAuthorizationHandler, ActiveUserAuthorizationHandler>();
builder.Services.AddScoped<IIdentityLoginGateway, IdentityLoginGateway>();
builder.Services.AddScoped<IdentityLoginService>();
builder.Services.AddRateLimiter(options =>
{
    ApprovalRateLimiting.Configure(options);
    AuthenticationRateLimiting.AddLoginPolicy(options);
});
if (builder.Environment.IsDevelopment())
    builder.Services.AddHostedService<DevelopmentIdentitySeedHostedService>();

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
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
