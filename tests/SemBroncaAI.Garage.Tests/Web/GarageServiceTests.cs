using System.Net;
using System.Text;
using SemBroncaAI.Garage.Web.Models;
using SemBroncaAI.Garage.Web.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class GarageServiceTests
{
    [Fact]
    public async Task Update_Should_Read_Json_Success_Response()
    {
        const string json = """{"id":"019fb7ee-1479-7f7b-95a1-496112008629","name":"Oficina","document":"123","phone":"1199","email":"a@b.com","postalCode":null,"street":null,"number":null,"complement":null,"neighborhood":null,"city":"Boituva","state":"SP","active":true,"createdAt":"2026-08-08T00:00:00Z"}""";
        var service = CreateService(HttpStatusCode.OK, "application/json", json);

        var result = await service.UpdateAsync(Request());

        result.Name.ShouldBe("Oficina"); result.State.ShouldBe("SP");
    }

    [Fact]
    public async Task Update_Should_Not_Deserialize_Plain_Text_Error_As_Json()
    {
        var service = CreateService(HttpStatusCode.InternalServerError, "text/plain",
            "Microsoft.EntityFrameworkCore.DbUpdateException: technical details");

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.UpdateAsync(Request()));

        exception.Message.ShouldBe("Não foi possível salvar as configurações.");
        exception.Message.ShouldNotContain("Json");
        exception.Message.ShouldNotContain("Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public async Task Update_Should_Preserve_Json_Api_Error_Message()
    {
        var service = CreateService(HttpStatusCode.BadRequest, "application/json", """{"message":"A cidade deve possuir no máximo 100 caracteres."}""");
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => service.UpdateAsync(Request()));
        exception.Message.ShouldBe("A cidade deve possuir no máximo 100 caracteres.");
    }

    private static UpdateGarageSettingsRequest Request() => new("Oficina", "123", "1199", "a@b.com", null, null, null, null, null, null, "sp", null);
    private static GarageService CreateService(HttpStatusCode status, string mediaType, string body) =>
        new(new HttpClient(new Handler(status, mediaType, body)) { BaseAddress = new Uri("http://localhost/") });

    private sealed class Handler(HttpStatusCode status, string mediaType, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) });
    }
}
