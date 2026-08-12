using SemBroncaAI.Garage.Application.Abstractions.Storage;

namespace SemBroncaAI.Garage.Infrastructure.Storage;

public sealed class LocalBrandAssetStorage : IBrandAssetStorage
{
    private readonly string _root;
    public LocalBrandAssetStorage(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _root = Path.GetFullPath(configuration["BrandAssets:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "brand-assets"));
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveLogoAsync(Guid garageId, Stream content, string extension, CancellationToken cancellationToken = default)
    {
        var key = $"{garageId:N}/{Guid.NewGuid():N}{extension}";
        var path = Resolve(key); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await content.CopyToAsync(destination, cancellationToken);
        return key;
    }

    public Task<BrandAsset?> OpenAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = Resolve(key);
        if (!File.Exists(path)) return Task.FromResult<BrandAsset?>(null);
        var contentType = Path.GetExtension(path).ToLowerInvariant() switch { ".png" => "image/png", ".jpg" => "image/jpeg", ".webp" => "image/webp", _ => "application/octet-stream" };
        return Task.FromResult<BrandAsset?>(new(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), contentType));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = Resolve(key); if (File.Exists(path)) File.Delete(path); return Task.CompletedTask;
    }

    private string Resolve(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Contains("..") || Path.IsPathRooted(key)) throw new InvalidOperationException("Chave de arquivo inválida.");
        var path = Path.GetFullPath(Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Chave de arquivo inválida.");
        return path;
    }
}
