using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.SiteManagement;
using SemBroncaAI.Garage.Domain.Entities.SiteManagement;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController,Route("api/platform/sites"),Authorize(Policy=PlatformAuthorization.Policy)]
public sealed class PlatformSitesController(IManagedSiteAdministration administration):ControllerBase
{
    [HttpGet] public Task<ManagedSiteDashboard> Dashboard([FromQuery]string? search,[FromQuery]ManagedSiteStatus? status,[FromQuery]ManagedSiteFinancialStatus? financialStatus,[FromQuery]string? hosting,[FromQuery]bool? active,CancellationToken token)=>administration.DashboardAsync(new(search,status,financialStatus,hosting,active),token);
    [HttpGet("{id:guid}")] public async Task<ActionResult<ManagedSiteDetails>> Get(Guid id,CancellationToken token){var value=await administration.GetAsync(id,token);return value is null?NotFound():Ok(value);}
    [HttpPost] public async Task<IActionResult> Create(ManagedSiteSaveCommand command,CancellationToken token){try{var id=await administration.CreateAsync(command,token);return CreatedAtAction(nameof(Get),new{id},new{id});}catch(ArgumentException e){return BadRequest(new{message=e.Message});}catch(Microsoft.EntityFrameworkCore.DbUpdateException){return Conflict(new{message="Já existe um site com este domínio."});}}
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id,ManagedSiteSaveCommand command,CancellationToken token){try{return await administration.UpdateAsync(id,command,token)?NoContent():NotFound();}catch(ArgumentException e){return BadRequest(new{message=e.Message});}catch(Microsoft.EntityFrameworkCore.DbUpdateException){return Conflict(new{message="Já existe um site com este domínio."});}}
    [HttpPut("{id:guid}/active")] public async Task<IActionResult> Active(Guid id,ActiveRequest request,CancellationToken token)=>await administration.SetActiveAsync(id,request.Active,token)?NoContent():NotFound();
    [HttpPost("{id:guid}/history")] public async Task<IActionResult> History(Guid id,HistoryRequest request,CancellationToken token){try{return await administration.AddHistoryAsync(id,request.Text,token)?NoContent():NotFound();}catch(ArgumentException e){return BadRequest(new{message=e.Message});}}
    public sealed record ActiveRequest(bool Active); public sealed record HistoryRequest(string Text);
}
