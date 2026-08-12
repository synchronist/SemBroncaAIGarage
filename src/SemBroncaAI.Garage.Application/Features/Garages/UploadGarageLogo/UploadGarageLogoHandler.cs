using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Abstractions.Storage;
using SemBroncaAI.Garage.Application.Features.Garages.GetGarageSettings;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.Garages.UploadGarageLogo;

public sealed class UploadGarageLogoHandler(IGarageRepository repository, IBrandAssetStorage storage, IUnitOfWork unitOfWork)
{
    public const long MaximumBytes = 2 * 1024 * 1024;

    public async Task<GetGarageSettingsResponse> HandleAsync(Guid garageId, Stream content, long length, string contentType, CancellationToken cancellationToken = default)
    {
        var garage = await repository.GetForUpdateAsync(garageId, cancellationToken)
            ?? throw new InvalidOperationException("Oficina não encontrada.");
        var extension = await DetectExtensionAsync(content, length, contentType, cancellationToken);
        var oldKey = garage.LogoStorageKey;
        var newKey = await storage.SaveLogoAsync(garageId, content, extension, cancellationToken);
        try
        {
            garage.UpdateBranding(newKey, garage.PrimaryColor);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(newKey, CancellationToken.None);
            throw;
        }
        if (!string.IsNullOrWhiteSpace(oldKey) && oldKey != newKey)
        {
            try { await storage.DeleteAsync(oldKey, cancellationToken); }
            catch { /* A nova logo já está persistida; limpeza pode ser repetida posteriormente. */ }
        }
        return new(garage.Id, garage.Name, garage.Document, garage.Phone, garage.Email, garage.PostalCode,
            garage.Street, garage.Number, garage.Complement, garage.Neighborhood, garage.City, garage.State,
            garage.LogoStorageKey, garage.PrimaryColor, garage.Active, garage.CreatedAt);
    }

    public static async Task<string> DetectExtensionAsync(Stream content, long length, string contentType, CancellationToken cancellationToken = default)
    {
        if (length <= 0 || length > MaximumBytes) throw new ArgumentException("A logo deve possuir no máximo 2 MB.");
        var header = new byte[12];
        var read = await content.ReadAsync(header, cancellationToken);
        if (content.CanSeek) content.Position = 0;
        var png = read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpeg = read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var webp = read >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
        return (contentType.ToLowerInvariant(), png, jpeg, webp) switch
        {
            ("image/png", true, _, _) => ".png",
            ("image/jpeg", _, true, _) => ".jpg",
            ("image/webp", _, _, true) => ".webp",
            _ => throw new ArgumentException("Envie uma logo PNG, JPEG ou WebP válida.")
        };
    }
}
