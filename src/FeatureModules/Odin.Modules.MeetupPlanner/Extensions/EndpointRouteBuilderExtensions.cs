using Infinity.Toolkit;
using Infinity.Toolkit.AspNetCore;
using Infinity.Toolkit.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Odin.Modules.MeetupPlanner.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder MapGetQuery<TResponse>(this IEndpointRouteBuilder builder, string path)
        where TResponse : class
    {
        return builder.MapGet(path, async ([FromServices] IRequestHandler<TResponse> requestHandler) =>
        {
            var result = await requestHandler.HandleAsync();

            IResult response = result switch
            {
                Success => TypedResults.Ok(result.Value),
                Failure => TypedResults.Problem(result.ToProblemDetails()),
                _ => TypedResults.BadRequest("Failed to process request.")
            };

            return response;
        })
        .Produces<TResponse>(statusCode: StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
