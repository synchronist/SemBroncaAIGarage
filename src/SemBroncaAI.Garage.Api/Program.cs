using System.Text.Json.Serialization;
using SemBroncaAI.Garage.Infrastructure;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SemBroncaAI.Garage.Application.Features.TeamManagement;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
ApiDeploymentConfiguration.Configure(builder);
builder.Services.AddScoped<IDocumentPdfGenerator, PlaywrightDocumentPdfGenerator>();
builder.Services.AddSingleton<IApprovalTokenService, ApprovalTokenService>();
builder.Services.AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme, options =>
    {
        options.BearerTokenExpiration = TimeSpan.FromDays(7);
        options.RefreshTokenExpiration = TimeSpan.FromDays(7);
    });
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in ApplicationPermissions.All)
        options.AddPolicy(permission, policy => policy
            .AddAuthenticationSchemes(IdentityConstants.BearerScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(new ActiveUserRequirement())
            .RequireClaim("garage_id")
            .RequireClaim(ApplicationPermissions.ClaimType, permission));
    var activeUserPolicy = new AuthorizationPolicyBuilder(IdentityConstants.BearerScheme)
        .RequireAuthenticatedUser()
        .AddRequirements(new ActiveUserRequirement())
        .Build();
    options.AddPolicy("ActiveUser", activeUserPolicy);
    options.AddPolicy(PlatformAuthorization.Policy, policy => policy
        .AddAuthenticationSchemes(IdentityConstants.BearerScheme)
        .RequireAuthenticatedUser()
        .AddRequirements(new ActiveUserRequirement())
        .RequireRole(ApplicationRoles.PlatformAdmin)
        .RequireAssertion(context => !context.User.HasClaim(claim => claim.Type == "garage_id")));
    options.AddPolicy("TenantUser", policy => policy
        .AddAuthenticationSchemes(IdentityConstants.BearerScheme)
        .RequireAuthenticatedUser()
        .AddRequirements(new ActiveUserRequirement())
        .RequireClaim("garage_id")
        .RequireAssertion(context => !context.User.IsInRole(ApplicationRoles.PlatformAdmin)));
    options.FallbackPolicy = activeUserPolicy;
});
builder.Services.AddScoped<IAuthorizationHandler, ActiveUserAuthorizationHandler>();
builder.Services.AddScoped<IIdentityLoginGateway, IdentityLoginGateway>();
builder.Services.AddScoped<IdentityLoginService>();
builder.Services.AddScoped<IPasswordRecoveryGateway, IdentityPasswordRecoveryGateway>();
builder.Services.AddScoped<PasswordRecoveryService>();
builder.Services.AddScoped<ServiceOrderConcurrencyExceptionFilter>();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IPasswordResetEmailSender, DevelopmentPasswordResetEmailSender>();
    builder.Services.AddScoped<ITeamInvitationSender, DevelopmentTeamInvitationSender>();
}
else
{
    builder.Services.AddScoped<IPasswordResetEmailSender, UnavailablePasswordResetEmailSender>();
    builder.Services.AddScoped<ITeamInvitationSender, UnavailableTeamInvitationSender>();
}
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpContextCurrentUser>();
builder.Services.AddScoped<SemBroncaAI.Garage.Application.Abstractions.Security.ICurrentUser>(sp => sp.GetRequiredService<HttpContextCurrentUser>());
builder.Services.AddScoped<SemBroncaAI.Garage.Application.Abstractions.Security.ICurrentGarage>(sp => sp.GetRequiredService<HttpContextCurrentUser>());
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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { message = ApiDeploymentConfiguration.UnexpectedErrorMessage });
    }));
    app.UseHsts();
}
app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
