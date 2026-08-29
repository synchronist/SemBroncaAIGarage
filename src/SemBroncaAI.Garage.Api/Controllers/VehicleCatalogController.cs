using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Api.Services;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController, Route("api/vehicle-catalog"), Authorize(Policy = "ActiveUser")]
public sealed class VehicleCatalogController(VehicleCatalogService catalog) : ControllerBase
{
    [HttpGet("brands")]
    public async Task<IActionResult> Brands(CancellationToken token)
    { try { return Ok(await catalog.BrandsAsync(token)); } catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException) { return StatusCode(503, new { message = "Catálogo temporariamente indisponível." }); } }
    [HttpGet("brands/{brandCode}/models")]
    public async Task<IActionResult> Models(string brandCode, CancellationToken token)
    { try { return Ok(await catalog.ModelsAsync(brandCode, token)); } catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException) { return StatusCode(503, new { message = "Catálogo temporariamente indisponível." }); } }
}
