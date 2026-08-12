using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Abstractions.Storage;
using SemBroncaAI.Garage.Application.Features.Garages.UploadGarageLogo;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Interfaces;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application.Garages;

public sealed class UploadGarageLogoHandlerTests
{
    [Fact]
    public async Task Should_Reject_File_Above_Limit()
    {
        await Should.ThrowAsync<ArgumentException>(() => UploadGarageLogoHandler.DetectExtensionAsync(
            new MemoryStream([1]), UploadGarageLogoHandler.MaximumBytes + 1, "image/png"));
    }

    [Fact]
    public async Task Should_Reject_Mime_That_Does_Not_Match_File_Signature()
    {
        await Should.ThrowAsync<ArgumentException>(() => UploadGarageLogoHandler.DetectExtensionAsync(
            new MemoryStream([0xFF, 0xD8, 0xFF, 0]), 4, "image/png"));
    }

    [Fact]
    public async Task Should_Save_Using_Garage_Isolation_And_Safe_Storage_Key()
    {
        var garage = new GarageEntity("Oficina", "123", "1199", "a@b.com");
        var repository = new Repository(garage); var storage = new Storage();
        var handler = new UploadGarageLogoHandler(repository, storage, new UnitOfWork());
        var png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0 };

        var response = await handler.HandleAsync(garage.Id, new MemoryStream(png), png.Length, "image/png");

        storage.GarageId.ShouldBe(garage.Id); storage.Key.ShouldNotContain("..");
        storage.Key.ShouldStartWith(garage.Id.ToString("N")); response.LogoStorageKey.ShouldBe(storage.Key);
    }

    private sealed class Repository(GarageEntity garage) : IGarageRepository
    {
        public Task<GarageEntity?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<GarageEntity?>(id == garage.Id ? garage : null);
        public Task<GarageEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => GetForUpdateAsync(id, cancellationToken);
        public Task AddAsync(GarageEntity entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsByDocumentAsync(string document, Guid? excludingGarageId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<GarageEntity>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GarageEntity>>([garage]);
    }
    private sealed class Storage : IBrandAssetStorage
    {
        public Guid GarageId { get; private set; } public string Key { get; private set; } = string.Empty;
        public Task<string> SaveLogoAsync(Guid garageId, Stream content, string extension, CancellationToken cancellationToken = default) { GarageId = garageId; Key = $"{garageId:N}/{Guid.NewGuid():N}{extension}"; return Task.FromResult(Key); }
        public Task<BrandAsset?> OpenAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult<BrandAsset?>(null);
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class UnitOfWork : IUnitOfWork { public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1); }
}
