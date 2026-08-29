using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using SemBroncaAI.Garage.Api.Controllers;
using SemBroncaAI.Garage.Api.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class VehicleCatalogTests
{
    [Fact]
    public async Task Catalog_should_map_brasil_api_and_cache_repeated_reads()
    {
        var handler = new CatalogHandler();
        var service = new VehicleCatalogService(new Factory(handler), new MemoryCache(new MemoryCacheOptions()));
        var brands = await service.BrandsAsync(default);
        var models = await service.ModelsAsync("59", default);
        (await service.BrandsAsync(default)).ShouldBe(brands);
        (await service.ModelsAsync("59", default)).ShouldBe(models);
        brands.Single().Name.ShouldBe("Volkswagen");
        models.ShouldContain("Gol");
        handler.Requests.ShouldBe(2);
    }

    [Fact]
    public void Catalog_endpoint_should_remain_authenticated() =>
        typeof(VehicleCatalogController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single().Policy.ShouldBe("ActiveUser");

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    { public HttpClient CreateClient(string name) => new(handler, false) { BaseAddress = new Uri("https://brasilapi.com.br/") }; }
    private sealed class CatalogHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            var json = request.RequestUri!.AbsolutePath.Contains("marcas") ? "[{\"nome\":\"Volkswagen\",\"valor\":\"59\"}]" : "[{\"modelo\":\"Gol\",\"valor\":\"1\"}]";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
        }
    }
}
