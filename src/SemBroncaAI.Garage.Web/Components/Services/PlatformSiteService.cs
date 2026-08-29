using System.Net.Http.Json;
using SemBroncaAI.Garage.Application.Features.SiteManagement;
using SemBroncaAI.Garage.Domain.Entities.SiteManagement;

namespace SemBroncaAI.Garage.Web.Services;
public sealed class PlatformSiteService(HttpClient http)
{
    public async Task<ManagedSiteDashboard> DashboardAsync(string? search,ManagedSiteStatus? status,ManagedSiteFinancialStatus? financial,string? hosting,bool? active,CancellationToken token=default)=>await http.GetFromJsonAsync<ManagedSiteDashboard>($"api/platform/sites?search={Uri.EscapeDataString(search??"")}&status={status}&financialStatus={financial}&hosting={Uri.EscapeDataString(hosting??"")}&active={active}",token)??throw new InvalidOperationException("A API não retornou os sites.");
    public async Task<ManagedSiteDetails?> GetAsync(Guid id,CancellationToken token=default){using var response=await http.GetAsync($"api/platform/sites/{id}",token);if(response.StatusCode==System.Net.HttpStatusCode.NotFound)return null;response.EnsureSuccessStatusCode();return await response.Content.ReadFromJsonAsync<ManagedSiteDetails>(cancellationToken:token);}
    public async Task<Guid> CreateAsync(ManagedSiteSaveCommand command,CancellationToken token=default){using var response=await http.PostAsJsonAsync("api/platform/sites",command,token);await Ensure(response,token);return (await response.Content.ReadFromJsonAsync<Created>(cancellationToken:token))!.Id;}
    public async Task UpdateAsync(Guid id,ManagedSiteSaveCommand command,CancellationToken token=default){using var response=await http.PutAsJsonAsync($"api/platform/sites/{id}",command,token);await Ensure(response,token);}
    public async Task SetActiveAsync(Guid id,bool active,CancellationToken token=default){using var response=await http.PutAsJsonAsync($"api/platform/sites/{id}/active",new{active},token);await Ensure(response,token);}
    public async Task AddHistoryAsync(Guid id,string text,CancellationToken token=default){using var response=await http.PostAsJsonAsync($"api/platform/sites/{id}/history",new{text},token);await Ensure(response,token);}
    private static async Task Ensure(HttpResponseMessage response,CancellationToken token){if(response.IsSuccessStatusCode)return;var error=await response.Content.ReadFromJsonAsync<Error>(cancellationToken:token);throw new InvalidOperationException(error?.Message??"Não foi possível concluir a operação.");}
    private sealed record Created(Guid Id);private sealed record Error(string Message);
}
