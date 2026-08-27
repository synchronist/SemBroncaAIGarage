using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Customers.CreateCustomer;
using SemBroncaAI.Garage.Application.Features.Customers.GetCustomerById;
using SemBroncaAI.Garage.Application.Features.Customers.ListCustomers;
using SemBroncaAI.Garage.Application.Features.Customers.UpdateCustomer;
using SemBroncaAI.Garage.Application.Features.Garages.CreateGarage;
using SemBroncaAI.Garage.Application.Features.Garages.GetGarageSettings;
using SemBroncaAI.Garage.Application.Features.Garages.UpdateGarageSettings;
using SemBroncaAI.Garage.Application.Features.Garages.UploadGarageLogo;
using SemBroncaAI.Garage.Application.Abstractions.Storage;
using SemBroncaAI.Garage.Infrastructure.Storage;
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
using SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.ArchiveServiceOrder;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.RestoreServiceOrder;
using SemBroncaAI.Garage.Application.Features.Estimates.ListEstimates;
using SemBroncaAI.Garage.Application.Features.Vehicles.CreateVehicle;
using SemBroncaAI.Garage.Application.Features.Vehicles.GetVehicleById;
using SemBroncaAI.Garage.Application.Features.Vehicles.ListVehicles;
using SemBroncaAI.Garage.Application.Features.Vehicles.UpdateVehicle;
using SemBroncaAI.Garage.Domain.Interfaces;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using SemBroncaAI.Garage.Infrastructure.Persistence.Repositories;
using SemBroncaAI.Garage.Infrastructure.Repositories;
using SemBroncaAI.Garage.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using SemBroncaAI.Garage.Application.Features.PlatformAdministration;
using SemBroncaAI.Garage.Application.Features.TeamManagement;
using SemBroncaAI.Garage.Application.Features.Subscriptions;

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

        services.AddIdentityCore<ApplicationUser>(ConfigureIdentity)
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<GarageDbContext>()
            .AddDefaultTokenProviders();
        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromHours(2));
        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();

        services.AddScoped<IDevelopmentIdentitySeedStore, IdentityDevelopmentSeedStore>();
        services.AddScoped<ApplicationIdentityRoleInitializer>();
        services.AddScoped<PlatformAdminBootstrapper>();
        services.AddScoped<IPlatformGarageAdministration, PlatformGarageAdministration>();
        services.AddOptions<SubscriptionOptions>()
            .Bind(configuration.GetSection(SubscriptionOptions.SectionName))
            .Validate(options => options.DefaultTrialDays > 0, "Subscription:DefaultTrialDays deve ser maior que zero.")
            .ValidateOnStart();
        services.AddScoped<ITeamManagement, TeamManagement>();
        services.AddScoped<IOwnerSubscriptionQuery, OwnerSubscriptionQuery>();
        services.AddOptions<StripeBillingOptions>()
            .Bind(configuration.GetSection(StripeBillingOptions.SectionName))
            .Validate(options => !options.Enabled ||
                ((options.SecretKey.StartsWith("sk_", StringComparison.Ordinal) ||
                  options.SecretKey.StartsWith("rk_", StringComparison.Ordinal)) &&
                 options.WebhookSecret.StartsWith("whsec_", StringComparison.Ordinal) &&
                 options.MonthlyPriceId.StartsWith("price_", StringComparison.Ordinal) &&
                 options.AnnualPriceId.StartsWith("price_", StringComparison.Ordinal) &&
                 !string.Equals(options.MonthlyPriceId, options.AnnualPriceId, StringComparison.Ordinal)),
                "A configuração Stripe habilitada deve conter SecretKey, WebhookSecret e os Price IDs mensal/anual.")
            .ValidateOnStart();
        services.AddScoped<StripeBillingService>();
        services.AddScoped<IOwnerBillingService>(sp => sp.GetRequiredService<StripeBillingService>());
        services.AddScoped<IBillingWebhookProcessor>(sp => sp.GetRequiredService<StripeBillingService>());
        services.AddScoped<IAuditWriter, AuditWriter>();

        services.AddScoped<IUnitOfWork>(sp =>
            sp.GetRequiredService<GarageDbContext>());

        services.AddScoped<IGarageRepository, GarageRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerQueryRepository, CustomerQueryRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IVehicleQueryRepository, VehicleQueryRepository>();
        services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
        services.AddScoped<IApprovalRequestPersistence, ApprovalRequestPersistence>();
        services.AddScoped<IEstimateQueryRepository, EstimateQueryRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<
    IServiceOrderQueryRepository,
    ServiceOrderQueryRepository>();

        services.AddScoped<
            IServiceOrderNumberGenerator,
            ServiceOrderNumberGenerator>();

        services.AddScoped<CreateGarageHandler>();
        services.AddScoped<GetGarageSettingsHandler>();
        services.AddScoped<UpdateGarageSettingsHandler>();
        services.AddScoped<UploadGarageLogoHandler>();
        services.AddSingleton<IBrandAssetStorage, LocalBrandAssetStorage>();
        services.AddScoped<CreateCustomerHandler>();
        services.AddScoped<ListCustomersHandler>();
        services.AddScoped<GetCustomerByIdHandler>();
        services.AddScoped<UpdateCustomerHandler>();
        services.AddScoped<CreateVehicleHandler>();
        services.AddScoped<ListVehiclesHandler>();
        services.AddScoped<GetVehicleByIdHandler>();
        services.AddScoped<UpdateVehicleHandler>();
        services.AddScoped<CreateServiceOrderHandler>();
        services.AddScoped<GetServiceOrderByIdHandler>();
        services.AddScoped<SemBroncaAI.Garage.Application.Features.ServiceOrders.GetTechnicalHistory.GetTechnicalHistoryHandler>();
        services.AddScoped<StartDiagnosisHandler>();
        services.AddScoped<SendForApprovalHandler>();
        services.AddScoped<SendEstimateForApprovalHandler>();
        services.AddScoped<PublicApprovalHandler>();
        services.AddScoped<ReviseEstimateHandler>();
        services.AddScoped<StartServiceHandler>();
        services.AddScoped<WaitForPartsHandler>();
        services.AddScoped<ResumeServiceHandler>();
        services.AddScoped<FinishServiceHandler>();
        services.AddScoped<DeliverServiceOrderHandler>();
        services.AddScoped<CancelServiceOrderHandler>();
        services.AddScoped<SearchLookupHandler>();
        services.AddScoped<ListServiceOrdersHandler>();
        services.AddScoped<ArchiveServiceOrderHandler>();
        services.AddScoped<RestoreServiceOrderHandler>();
        services.AddScoped<ListEstimatesHandler>();
        services.AddScoped<SaveDiagnosisHandler>();
        services.AddScoped<SaveEstimateHandler>();

        return services;
    }

    public static void ConfigureIdentity(IdentityOptions options)
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 4;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    }
}
