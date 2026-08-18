using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace SemBroncaAI.Garage.Api.Services;

public sealed class ServiceOrderConcurrencyExceptionFilter : IExceptionFilter
{
    public const string Message =
        "A ordem de serviço foi alterada por outra operação. Atualize a página e tente novamente.";

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DbUpdateConcurrencyException)
            return;

        context.Result = new ConflictObjectResult(new { message = Message });
        context.ExceptionHandled = true;
    }
}
