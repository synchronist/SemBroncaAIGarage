using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Domain.Entities.Customer;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using SemBroncaAI.Garage.Infrastructure.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Integration;

public sealed class OperationalDashboardPostgresTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("SBGARAGE_INTEGRATION_CONNECTION");

    [PostgresFact]
    [Trait("Category", "PostgreSQLIntegration")]
    public async Task Dashboard_should_calculate_operational_and_monthly_metrics_without_cross_tenant_leakage()
    {
        var first = CreateTenant("DA");
        var second = CreateTenant("DB");
        var now = DateTimeOffset.UtcNow;
        var received = NewOrder(first.Garage.Id, first.Vehicle.Id, 1);
        var waiting = NewOrder(first.Garage.Id, first.Vehicle.Id, 2);
        waiting.StartDiagnosis(); waiting.SaveDiagnosis("Diagnóstico");
        waiting.SaveEstimate([new("Serviço", EstimateItemType.Service, 1, 200m)]);
        waiting.SendForApproval("A".PadLeft(64, 'A'), "token-a", now.AddDays(7), now);
        var completed = CompletedOrder(first.Garage.Id, first.Vehicle.Id, 3, 300m, "B", now);
        var foreign = CompletedOrder(second.Garage.Id, second.Vehicle.Id, 1, 900m, "C", now);

        await using (var setup = CreateContext())
        {
            setup.AddRange(first.Garage, first.Customer, first.Vehicle, second.Garage, second.Customer, second.Vehicle,
                received, waiting, completed, foreign);
            await setup.SaveChangesAsync();
        }

        try
        {
            await using var context = CreateContext();
            var query = new OperationalDashboardQuery(context);
            var result = await query.GetAsync(first.Garage.Id, true, now.AddMinutes(1));

            result.Counters.Open.ShouldBe(3);
            result.Counters.WaitingApproval.ShouldBe(1);
            result.Counters.ReadyForDelivery.ShouldBe(1);
            result.Counters.EntriesToday.ShouldBe(3);
            result.MonthlySummary.ShouldNotBeNull();
            result.MonthlySummary.Completed.ShouldBe(1);
            result.MonthlySummary.ApprovedEstimateValue.ShouldBe(300m);
            result.MonthlySummary.AverageTicket.ShouldBe(300m);
            result.MonthlySummary.ApprovalRate.ShouldBe(100m);
            result.RecentActivity.ShouldAllBe(item => item.ServiceOrderId != foreign.Id);
            result.DailyCompletions.Sum(day => day.Count).ShouldBe(1);

            (await query.GetAsync(first.Garage.Id, false, now.AddMinutes(1))).MonthlySummary.ShouldBeNull();
        }
        finally
        {
            await using var cleanup = CreateContext();
            var ids = new[] { first.Garage.Id, second.Garage.Id };
            await cleanup.ServiceOrders.Where(x => ids.Contains(x.GarageId)).ExecuteDeleteAsync();
            await cleanup.Vehicles.Where(x => ids.Contains(x.GarageId)).ExecuteDeleteAsync();
            await cleanup.Customers.Where(x => ids.Contains(x.GarageId)).ExecuteDeleteAsync();
            await cleanup.Garages.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync();
        }
    }

    private static (GarageEntity Garage, CustomerEntity Customer, VehicleEntity Vehicle) CreateTenant(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var garage = new GarageEntity($"Dashboard {prefix}", $"{prefix}{suffix}"[..20], "11999999999", $"{prefix}-{suffix}@test.local");
        var customer = new CustomerEntity(garage.Id, $"Cliente {prefix}", $"C{suffix}"[..20], "11999999999", $"c-{suffix}@test.local");
        var vehicle = new VehicleEntity(garage.Id, customer.Id, $"{prefix}{suffix[..5]}".ToUpperInvariant(), "Marca", "Modelo", "V1", 2026, "Preto", "Flex", 10);
        return (garage, customer, vehicle);
    }

    private static ServiceOrderEntity NewOrder(Guid garageId, Guid vehicleId, int number) =>
        new(garageId, vehicleId, number, "Teste dashboard", 10);

    private static ServiceOrderEntity CompletedOrder(Guid garageId, Guid vehicleId, int number, decimal total,
        string token, DateTimeOffset now)
    {
        var order = NewOrder(garageId, vehicleId, number);
        order.StartDiagnosis(); order.SaveDiagnosis("Diagnóstico");
        order.SaveEstimate([new("Serviço", EstimateItemType.Service, 1, total)]);
        var approval = order.SendForApproval(token.PadLeft(64, token[0]), $"token-{token}", now.AddDays(7), now);
        order.ApproveEstimate(approval.Id, "Cliente", now.AddSeconds(1));
        order.StartService(); order.Finish();
        return order;
    }

    private static GarageDbContext CreateContext() => new(
        new DbContextOptionsBuilder<GarageDbContext>().UseNpgsql(ConnectionString).Options);
}
