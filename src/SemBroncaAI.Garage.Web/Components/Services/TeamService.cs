using System.Net.Http.Json;
using SemBroncaAI.Garage.Application.Features.TeamManagement;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class TeamService(HttpClient client)
{
    public Task<TeamMemberItem[]?> ListAsync(string? search = null) => client.GetFromJsonAsync<TeamMemberItem[]>($"api/team?search={Uri.EscapeDataString(search ?? "")}");
    public async Task<TeamMemberDetails?> GetAsync(Guid id) { using var r = await client.GetAsync($"api/team/{id}"); return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<TeamMemberDetails>() : null; }
    public async Task<TeamOperationResult> InviteAsync(InviteTeamMemberCommand command) => await Send(await client.PostAsJsonAsync("api/team/invite", command));
    public async Task<TeamOperationResult> UpdateAsync(Guid id, UpdateTeamMemberCommand command) => await Send(await client.PutAsJsonAsync($"api/team/{id}", command));
    private static async Task<TeamOperationResult> Send(HttpResponseMessage response) { using (response) { return response.IsSuccessStatusCode ? new(true) : await response.Content.ReadFromJsonAsync<TeamOperationResult>() ?? new(false, "invalid"); } }
}

public sealed class TeamInvitationService(HttpClient client)
{
    public async Task<TeamOperationResult> AcceptAsync(AcceptTeamInvitationCommand command) { using var response = await client.PostAsJsonAsync("api/team-invitations/accept", command); return response.IsSuccessStatusCode ? new(true) : await response.Content.ReadFromJsonAsync<TeamOperationResult>() ?? new(false, "invalid"); }
}
