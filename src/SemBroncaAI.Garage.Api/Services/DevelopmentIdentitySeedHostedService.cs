using SemBroncaAI.Garage.Infrastructure.Identity;

namespace SemBroncaAI.Garage.Api.Services;

public sealed class DevelopmentIdentitySeedHostedService(
    IServiceProvider services,
    IConfiguration configuration) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) =>
        services.SeedDevelopmentIdentityAsync(configuration, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
