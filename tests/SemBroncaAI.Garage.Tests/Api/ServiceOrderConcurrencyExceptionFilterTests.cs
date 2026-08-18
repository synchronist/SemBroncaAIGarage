using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Api.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class ServiceOrderConcurrencyExceptionFilterTests
{
    [Fact]
    public void Stale_service_order_write_should_become_controlled_conflict()
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var context = new ExceptionContext(actionContext, [])
        {
            Exception = new DbUpdateConcurrencyException("internal database detail")
        };

        new ServiceOrderConcurrencyExceptionFilter().OnException(context);

        context.ExceptionHandled.ShouldBeTrue();
        var result = context.Result.ShouldBeOfType<ConflictObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        var responseText = result.Value!.ToString()!;
        responseText.ShouldNotContain("internal database detail");
        responseText.ShouldContain("Atualize a página");
    }
}
