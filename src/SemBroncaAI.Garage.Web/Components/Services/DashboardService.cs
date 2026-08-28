using System.Net.Http.Json;
using SemBroncaAI.Garage.Application.Features.Dashboard;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class DashboardService(HttpClient httpClient)
{
    public async Task<OperationalDashboardResponse> GetAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<OperationalDashboardResponse>("api/dashboard", cancellationToken)
        ?? throw new InvalidOperationException("A API não retornou os dados do dashboard.");
}
