using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Customers.CreateCustomer;
using SemBroncaAI.Garage.Application.Features.Garages.CreateGarage;
using SemBroncaAI.Garage.Application.Features.Lookup;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.CancelServiceOrder;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.CreateServiceOrder;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.DeliverServiceOrder;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.FinishService;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.GetServiceOrderById;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.ResumeService;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.SaveDiagnosis;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.SaveEstimate;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.SendForApproval;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.StartDiagnosis;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.StartService;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.WaitForParts;
using SemBroncaAI.Garage.Application.Features.Vehicles.CreateVehicle;
using SemBroncaAI.Garage.Domain.Interfaces;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using SemBroncaAI.Garage.Infrastructure.Persistence.Repositories;
using SemBroncaAI.Garage.Infrastructure.Repositories;
using SemBroncaAI.Garage.Infrastructure.Services;

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
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<
    IServiceOrderQueryRepository,
    ServiceOrderQueryRepository>();

        services.AddScoped<
            IServiceOrderNumberGenerator,
            ServiceOrderNumberGenerator>();

        services.AddScoped<CreateGarageHandler>();
        services.AddScoped<CreateCustomerHandler>();
        services.AddScoped<CreateVehicleHandler>();
        services.AddScoped<CreateServiceOrderHandler>();
        services.AddScoped<GetServiceOrderByIdHandler>();
        services.AddScoped<StartDiagnosisHandler>();
        services.AddScoped<SendForApprovalHandler>();
        services.AddScoped<StartServiceHandler>();
        services.AddScoped<WaitForPartsHandler>();
        services.AddScoped<ResumeServiceHandler>();
        services.AddScoped<FinishServiceHandler>();
        services.AddScoped<DeliverServiceOrderHandler>();
        services.AddScoped<CancelServiceOrderHandler>();
        services.AddScoped<SearchLookupHandler>();
        services.AddScoped<ListServiceOrdersHandler>();
        services.AddScoped<SaveDiagnosisHandler>();
        services.AddScoped<SaveEstimateHandler>();

        return services;
    }
}
