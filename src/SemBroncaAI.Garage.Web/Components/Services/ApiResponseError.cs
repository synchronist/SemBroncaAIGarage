using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SemBroncaAI.Garage.Web.Services;

internal static class ApiResponseError
{
    public static async Task ThrowAsync(
        HttpResponseMessage response,
        string operationMessage,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Sua sessão expirou. Entre novamente para continuar.");

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new InvalidOperationException("Você não tem permissão para executar esta ação.");

        string? apiMessage = null;
        if (response.Content.Headers.ContentLength is not 0)
        {
            try
            {
                apiMessage = (await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: cancellationToken))?.Message;
            }
            catch (JsonException)
            {
                // Respostas sem o contrato JSON da API não devem vazar detalhes técnicos para a interface.
            }
            catch (NotSupportedException)
            {
                // Respostas não JSON são convertidas para uma mensagem operacional segura.
            }
        }

        throw new InvalidOperationException(
            response.StatusCode >= HttpStatusCode.InternalServerError
                ? $"{operationMessage} Tente novamente em instantes."
                : apiMessage ?? operationMessage);
    }

    private sealed record ApiError(string? Message);
}
