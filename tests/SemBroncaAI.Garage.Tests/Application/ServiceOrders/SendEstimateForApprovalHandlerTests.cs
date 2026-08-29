using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Interfaces;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application.ServiceOrders;

public sealed class SendEstimateForApprovalHandlerTests
{
    [Fact]
    public async Task Normal_send_should_return_the_new_candidate_link()
    {
        var order = ReadyOrder();
        var tokens = new Tokens();
        var persistence = new CandidatePersistence();
        var handler = new SendEstimateForApprovalHandler(new Repository(order), tokens, persistence, new Validity());

        var response = await handler.HandleAsync(order.Id);

        response.Token.ShouldBe("candidate-public-token");
        tokens.UnprotectedValue.ShouldBeNull();
    }

    [Fact]
    public async Task Concurrent_loser_should_return_the_winning_active_link()
    {
        var candidateOrder = ReadyOrder();
        var winningOrder = ReadyOrder();
        var now = DateTimeOffset.UtcNow;
        var winner = winningOrder.SendForApproval(
            "B".PadLeft(64, 'B'), "protected-winner", now.AddDays(7), now);
        var persistence = new Persistence(winner);
        var tokens = new Tokens();
        var handler = new SendEstimateForApprovalHandler(
            new Repository(candidateOrder), tokens, persistence, new Validity());

        var response = await handler.HandleAsync(candidateOrder.Id);

        response.Token.ShouldBe("winning-public-token");
        persistence.Candidate.ShouldNotBeNull();
        persistence.Candidate!.Id.ShouldNotBe(winner.Id);
        tokens.UnprotectedValue.ShouldBe("protected-winner");
    }

    private static ServiceOrderEntity ReadyOrder()
    {
        var order = new ServiceOrderEntity(Guid.NewGuid(), Guid.NewGuid(), 1, "Ruído", 100);
        order.StartDiagnosis();
        order.SaveDiagnosis("Diagnóstico");
        order.SaveEstimate([new("Serviço", EstimateItemType.Service, 1, 100m)]);
        return order;
    }

    private sealed class Repository(ServiceOrderEntity order) : IServiceOrderRepository
    {
        public Task<ServiceOrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceOrderEntity?>(id == order.Id ? order : null);
        public Task<ServiceOrderEntity?> GetByNumberAsync(Guid garageId, int number, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceOrderEntity?>(null);
        public Task AddAsync(ServiceOrderEntity serviceOrder, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public void RemoveEstimateItems(IEnumerable<ServiceOrderEstimateItemEntity> items) { }
    }

    private sealed class Persistence(ServiceOrderEstimateApprovalEntity winner) : IApprovalRequestPersistence
    {
        public ServiceOrderEstimateApprovalEntity? Candidate { get; private set; }
        public Task<ServiceOrderEstimateApprovalEntity> SaveAsync(
            ServiceOrderEstimateApprovalEntity candidate,
            CancellationToken cancellationToken = default)
        {
            Candidate = candidate;
            return Task.FromResult(winner);
        }
    }

    private sealed class CandidatePersistence : IApprovalRequestPersistence
    {
        public Task<ServiceOrderEstimateApprovalEntity> SaveAsync(
            ServiceOrderEstimateApprovalEntity candidate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(candidate);
    }

    private sealed class Tokens : IApprovalTokenService
    {
        public string? UnprotectedValue { get; private set; }
        public ApprovalToken Create() => new("candidate-public-token", "A".PadLeft(64, 'A'), "protected-candidate");
        public string Hash(string token) => "hash";
        public string Unprotect(string protectedToken)
        {
            UnprotectedValue = protectedToken;
            return "winning-public-token";
        }
    }

    private sealed class Validity : IApprovalValidityProvider
    {
        public int DefaultValidityDays => 7;
    }
}
