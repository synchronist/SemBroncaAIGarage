using SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;
using Shouldly;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Tests.Application.ServiceOrders;

public sealed class PublicApprovalContractTests
{
    [Fact]
    public void Public_Contract_Should_Not_Expose_Internal_Or_Tenant_Data()
    {
        typeof(PublicApprovalResponse).GetProperty("InternalNotes").ShouldBeNull();
        typeof(PublicApprovalResponse).GetProperty("GarageId").ShouldBeNull();
        typeof(PublicApprovalResponse).GetProperty("ServiceOrderId").ShouldBeNull();
        typeof(PublicApprovalResponse).GetProperty("CustomerId").ShouldBeNull();
    }

    [Fact]
    public async Task Invalid_Token_Should_Not_Resolve_An_Approval()
    {
        var handler = new PublicApprovalHandler(new EmptyRepository(), new TokenService(), new UnitOfWork());
        (await handler.GetAsync("invalid-token", null)).ShouldBeNull();
    }

    private sealed class TokenService : IApprovalTokenService
    {
        public ApprovalToken Create() => throw new NotSupportedException();
        public string Hash(string token) => "HASH";
        public string Unprotect(string protectedToken) => throw new NotSupportedException();
    }
    private sealed class UnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
    private sealed class EmptyRepository : IServiceOrderRepository
    {
        public Task<ServiceOrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ServiceOrderEntity?>(null);
        public Task<ServiceOrderEntity?> GetByNumberAsync(Guid garageId, int number, CancellationToken cancellationToken = default) => Task.FromResult<ServiceOrderEntity?>(null);
        public Task<ServiceOrderEntity?> GetByApprovalTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) => Task.FromResult<ServiceOrderEntity?>(null);
        public Task AddAsync(ServiceOrderEntity serviceOrder, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void RemoveEstimateItems(IEnumerable<ServiceOrderEstimateItemEntity> items) { }
    }
}
