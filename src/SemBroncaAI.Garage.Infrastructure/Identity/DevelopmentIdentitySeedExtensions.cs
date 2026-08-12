using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SemBroncaAI.Garage.Infrastructure.Identity;

public static class DevelopmentIdentitySeedExtensions
{
    public static async Task SeedDevelopmentIdentityAsync(
        this IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var options = new DevelopmentIdentitySeedOptions();
        configuration.GetSection(DevelopmentIdentitySeedOptions.SectionName).Bind(options);
        if (!options.Enabled) return;

        await using var scope = services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IDevelopmentIdentitySeedStore>();
        await new DevelopmentIdentitySeeder(store).SeedAsync(options, cancellationToken);
    }
}
