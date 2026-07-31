using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Customers.CreateCustomer;
using SemBroncaAI.Garage.Application.Features.Garages.CreateGarage;
using SemBroncaAI.Garage.Domain.Interfaces;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using SemBroncaAI.Garage.Infrastructure.Persistence.Repositories;
using SemBroncaAI.Garage.Infrastructure.Repositories;

namespace SemBroncaAI.Garage.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration
            .GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A connection string 'DefaultConnection' não foi encontrada.");

        services.AddDbContext<GarageDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(sp =>
            sp.GetRequiredService<GarageDbContext>());

        services.AddScoped<IGarageRepository, GarageRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        services.AddScoped<CreateCustomerHandler>();
        services.AddScoped<CreateGarageHandler>();

        return services;
    }
}