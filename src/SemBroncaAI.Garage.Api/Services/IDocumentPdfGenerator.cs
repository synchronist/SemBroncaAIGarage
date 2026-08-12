namespace SemBroncaAI.Garage.Api.Services;

public interface IDocumentPdfGenerator
{
    Task<byte[]> GenerateAsync(string relativeDocumentUrl, string readySelector, CancellationToken cancellationToken = default);
}
