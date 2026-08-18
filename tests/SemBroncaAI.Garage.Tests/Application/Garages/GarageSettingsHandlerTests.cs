using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Garages.GetGarageSettings;
using SemBroncaAI.Garage.Application.Features.Garages.UpdateGarageSettings;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Interfaces;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application.Garages;

public sealed class GarageSettingsHandlerTests
{
    [Fact]
    public async Task Get_Should_Return_Requested_Garage()
    {
        var garage = Create(); var repository = new Repository(garage);
        var response = await new GetGarageSettingsHandler(repository).HandleAsync(garage.Id);
        response.ShouldNotBeNull(); response.Id.ShouldBe(garage.Id); response.Name.ShouldBe(garage.Name);
    }

    [Fact]
    public async Task Update_Should_Respect_Requested_Id_And_Exclude_It_From_Document_Check()
    {
        var garage = Create(); var repository = new Repository(garage); var unitOfWork = new UnitOfWork();
        var audit = new AuditWriter();
        var handler = new UpdateGarageSettingsHandler(repository, unitOfWork, audit);
        var response = await handler.HandleAsync(garage.Id, new("Oficina Nova", garage.Document, "1188", "novo@oficina.com", null, "Rua A", "10", null, "Centro", "Boituva", "SP", "#F97316"));

        repository.RequestedId.ShouldBe(garage.Id); repository.ExcludedId.ShouldBe(garage.Id);
        response.Name.ShouldBe("Oficina Nova"); response.City.ShouldBe("Boituva"); unitOfWork.SaveCount.ShouldBe(1);
        audit.Actions.ShouldBe([AuditActions.GarageSettingsChanged]);
    }

    [Fact]
    public async Task Update_Should_Not_Change_Another_Garage()
    {
        var garage = Create(); var handler = new UpdateGarageSettingsHandler(new Repository(garage), new UnitOfWork(), new AuditWriter());
        await Should.ThrowAsync<InvalidOperationException>(() => handler.HandleAsync(Guid.CreateVersion7(),
            new("Outra", "999", "1188", "a@b.com", null, null, null, null, null, null, null, null)));
        garage.Name.ShouldBe("Oficina");
    }

    private static GarageEntity Create() => new("Oficina", "123", "1199", "a@b.com");

    private sealed class Repository(GarageEntity garage) : IGarageRepository
    {
        public Guid? RequestedId { get; private set; } public Guid? ExcludedId { get; private set; }
        public Task<GarageEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<GarageEntity?>(id == garage.Id ? garage : null);
        public Task<GarageEntity?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) { RequestedId = id; return GetByIdAsync(id, cancellationToken); }
        public Task<bool> ExistsByDocumentAsync(string document, Guid? excludingGarageId = null, CancellationToken cancellationToken = default) { ExcludedId = excludingGarageId; return Task.FromResult(false); }
        public Task AddAsync(GarageEntity entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<GarageEntity>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GarageEntity>>([garage]);
    }
    private sealed class UnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { SaveCount++; return Task.FromResult(1); }
    }
    private sealed class AuditWriter : IAuditWriter
    {
        public List<string> Actions { get; } = [];
        public void Add(Guid? garageId, string action, string entityType, string entityId, string? summary = null) => Actions.Add(action);
    }
}
