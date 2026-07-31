using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Application.Features.Garages.CreateGarage;

namespace SemBroncaAI.Garage.Api.Controllers;

[ApiController]
[Route("api/garages")]
public class GaragesController : ControllerBase
{
    private readonly CreateGarageHandler _handler;

    public GaragesController(CreateGarageHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IResult> Create(
        CreateGarageCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _handler.HandleAsync(command, cancellationToken);

        return Results.Created($"/api/garages/{response.Id}", response);
    }
}