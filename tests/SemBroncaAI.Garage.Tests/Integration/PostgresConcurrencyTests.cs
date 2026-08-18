using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;
using SemBroncaAI.Garage.Domain.Entities.Customer;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using SemBroncaAI.Garage.Domain.Interfaces;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using SemBroncaAI.Garage.Infrastructure.Repositories;
using SemBroncaAI.Garage.Infrastructure.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Integration;

public sealed class PostgresConcurrencyTests
{
    private static readonly string? ConnectionString =
        Environment.GetEnvironmentVariable("SBGARAGE_INTEGRATION_CONNECTION");

    [PostgresFact]
    [Trait("Category", "PostgreSQLIntegration")]
    public async Task Number_reservations_should_be_unique_and_independent_per_garage()
    {
        var first = new GarageEntity("QA B02 A", $"QA{Guid.NewGuid():N}"[..20], "11999999999", "qa-b02-a@local.test");
        var second = new GarageEntity("QA B02 B", $"QB{Guid.NewGuid():N}"[..20], "11999999999", "qa-b02-b@local.test");
        await using (var setup = CreateContext())
        {
            setup.Garages.AddRange(first, second);
            await setup.SaveChangesAsync();
        }

        try
        {
            var firstNumbers = await ReserveAsync(first.Id, 20);
            var secondNumbers = await ReserveAsync(second.Id, 12);

            firstNumbers.Distinct().Count().ShouldBe(20);
            secondNumbers.Distinct().Count().ShouldBe(12);
            firstNumbers.Order().ShouldBe(Enumerable.Range(1, 20));
            secondNumbers.Order().ShouldBe(Enumerable.Range(1, 12));

            await using var verify = CreateContext();
            (await verify.ServiceOrderNumberSequences.SingleAsync(x => x.GarageId == first.Id)).LastNumber.ShouldBe(20);
            (await verify.ServiceOrderNumberSequences.SingleAsync(x => x.GarageId == second.Id)).LastNumber.ShouldBe(12);
        }
        finally
        {
            await using var cleanup = CreateContext();
            await cleanup.Garages.Where(x => x.Id == first.Id || x.Id == second.Id).ExecuteDeleteAsync();
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgreSQLIntegration")]
    public async Task Concurrent_approval_sends_should_converge_to_one_active_link()
    {
        var garage = new GarageEntity("QA B03", $"QC{Guid.NewGuid():N}"[..20], "11999999999", "qa-b03@local.test");
        var customer = new CustomerEntity(garage.Id, "QA B03", $"QD{Guid.NewGuid():N}"[..20], "11999999999", "qa-b03@local.test");
        var vehicle = new VehicleEntity(garage.Id, customer.Id, $"Q{Random.Shared.Next(100000, 999999)}A", "QA", "B03", "Test", 2026, "Preto", "Flex", 1);
        var order = new ServiceOrderEntity(garage.Id, vehicle.Id, 1, "QA B03", 1);
        order.StartDiagnosis();
        order.SaveDiagnosis("Diagnóstico QA");
        order.SaveEstimate([new("Serviço QA", EstimateItemType.Service, 1, 100m)]);

        await using (var setup = CreateContext())
        {
            setup.AddRange(garage, customer, vehicle, order);
            await setup.SaveChangesAsync();
        }

        try
        {
            using var barrier = new Barrier(4);
            var tasks = Enumerable.Range(1, 4).Select(index => Task.Run(async () =>
            {
                await using var context = CreateContext();
                var repository = new BarrierRepository(new ServiceOrderRepository(context), barrier);
                var handler = new SendEstimateForApprovalHandler(
                    repository,
                    new Tokens(index),
                    new ApprovalRequestPersistence(context));
                return await handler.HandleAsync(order.Id);
            }));

            var responses = await Task.WhenAll(tasks);

            responses.Select(x => x.Token).Distinct().Count().ShouldBe(1);
            await using var verify = CreateContext();
            var pending = await verify.ServiceOrderEstimateApprovals.CountAsync(x =>
                x.ServiceOrderId == order.Id &&
                x.EstimateUpdatedAt == order.Estimate!.UpdatedAt &&
                x.Status == EstimateApprovalStatus.Pending &&
                x.InvalidatedAt == null);
            pending.ShouldBe(1);
        }
        finally
        {
            await using var cleanup = CreateContext();
            await cleanup.ServiceOrders.Where(x => x.GarageId == garage.Id).ExecuteDeleteAsync();
            await cleanup.Vehicles.Where(x => x.GarageId == garage.Id).ExecuteDeleteAsync();
            await cleanup.Customers.Where(x => x.GarageId == garage.Id).ExecuteDeleteAsync();
            await cleanup.Garages.Where(x => x.Id == garage.Id).ExecuteDeleteAsync();
        }
    }

    private static async Task<int[]> ReserveAsync(Guid garageId, int count) =>
        await Task.WhenAll(Enumerable.Range(0, count).Select(async _ =>
        {
            await using var context = CreateContext();
            return await new ServiceOrderNumberGenerator(context).GetNextAsync(garageId);
        }));

    private static GarageDbContext CreateContext() => new(
        new DbContextOptionsBuilder<GarageDbContext>().UseNpgsql(ConnectionString).Options);

    private sealed class BarrierRepository(ServiceOrderRepository inner, Barrier barrier)
        : IServiceOrderRepository
    {
        public async Task<ServiceOrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var order = await inner.GetByIdAsync(id, cancellationToken);
            barrier.SignalAndWait(cancellationToken);
            return order;
        }

        public Task<ServiceOrderEntity?> GetByNumberAsync(Guid garageId, int number, CancellationToken cancellationToken = default) =>
            inner.GetByNumberAsync(garageId, number, cancellationToken);
        public Task AddAsync(ServiceOrderEntity serviceOrder, CancellationToken cancellationToken = default) =>
            inner.AddAsync(serviceOrder, cancellationToken);
        public void RemoveEstimateItems(IEnumerable<ServiceOrderEstimateItemEntity> items) => inner.RemoveEstimateItems(items);
    }

    private sealed class Tokens(int index) : IApprovalTokenService
    {
        public ApprovalToken Create()
        {
            var value = $"qa-token-{index}-{Guid.NewGuid():N}";
            return new(value, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))), value);
        }
        public string Hash(string token) => token;
        public string Unprotect(string protectedToken) => protectedToken;
    }
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SBGARAGE_INTEGRATION_CONNECTION")))
            Skip = "Defina SBGARAGE_INTEGRATION_CONNECTION para executar contra um PostgreSQL isolado.";
    }
}
