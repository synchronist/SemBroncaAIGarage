namespace SemBroncaAI.Garage.Application.Abstractions.Storage;

public interface IBrandAssetStorage
{
    Task<string> SaveLogoAsync(Guid garageId, Stream content, string extension, CancellationToken cancellationToken = default);
    Task<BrandAsset?> OpenAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

public sealed record BrandAsset(Stream Content, string ContentType);
