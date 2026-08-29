using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace SemBroncaAI.Garage.Api.Services;

public sealed record VehicleCatalogBrand(string Code, string Name);

public sealed class VehicleCatalogService(IHttpClientFactory clients, IMemoryCache cache)
{
    private const string BrandsKey = "vehicle-catalog:brands:cars";
    public async Task<IReadOnlyCollection<VehicleCatalogBrand>> BrandsAsync(CancellationToken token)
    {
        if (cache.TryGetValue(BrandsKey, out VehicleCatalogBrand[]? cached) && cached is not null) return cached;
        var source = await clients.CreateClient("BrasilApi").GetFromJsonAsync<BrasilApiBrand[]>("api/fipe/marcas/v1/carros", token) ?? [];
        var result = source.Select(x => new VehicleCatalogBrand(x.Valor, x.Nome)).OrderBy(x => x.Name).ToArray();
        cache.Set(BrandsKey, result, TimeSpan.FromHours(24));
        return result;
    }
    public async Task<IReadOnlyCollection<string>> ModelsAsync(string brandCode, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(brandCode) || brandCode.Any(x => !char.IsDigit(x))) return [];
        var key = $"vehicle-catalog:models:cars:{brandCode}";
        if (cache.TryGetValue(key, out string[]? cached) && cached is not null) return cached;
        var source = await clients.CreateClient("BrasilApi").GetFromJsonAsync<BrasilApiModel[]>($"api/fipe/veiculos/v1/carros/{brandCode}", token) ?? [];
        var result = source.Select(x => x.Modelo.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        cache.Set(key, result, TimeSpan.FromHours(24));
        return result;
    }
    private sealed record BrasilApiBrand(string Nome, string Valor);
    private sealed record BrasilApiModel(string Modelo, string Valor);
}
