using SemBroncaAI.Garage.Infrastructure.Identity;

namespace SemBroncaAI.Garage.Api.Services;

public sealed class ApplicationIdentityRoleHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationIdentityRoleInitializer>()
            .InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
