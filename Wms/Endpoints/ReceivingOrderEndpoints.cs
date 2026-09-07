using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Wms.Application.ReceivingOrders;
using Wms.Common;
using Wms.Data;

namespace Wms.Endpoints;

internal static class ReceivingOrderEndpoints
{
    public static IEndpointRouteBuilder MapReceivingOrderEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("/api")
            .WithTags("ReceivingOrder")
            .ProducesValidationProblem()
            .RequireAuthorization(policy => policy.RequireRole(ApplicationRoles.All));

        group.MapGet("/ReceivingOrder/{id:guid}", GetOrder);
        group.MapPost("/ReceivingOrder/{id:guid}/set-in-receiving", SetInReceiving)
            .Produces(StatusCodes.Status200OK)
            .Produces<ReceivingOrderCommandProblem>(StatusCodes.Status400BadRequest)
            .Produces<ReceivingOrderCommandProblem>(StatusCodes.Status404NotFound)
            .Produces<ReceivingOrderCommandProblem>(StatusCodes.Status409Conflict)
            .Produces<ReceivingOrderCommandProblem>(StatusCodes.Status422UnprocessableEntity);
        group.MapPost("/ReceivingOrder/{id:guid}/set-received", SetReceived)
            .Produces(StatusCodes.Status200OK)
            .Produces<ReceivingOrderCommandProblem>(StatusCodes.Status400BadRequest)
            .Produces<ReceivingOrderCommandProblem>(StatusCodes.Status404NotFound)
            .Produces<ReceivingOrderCommandProblem>(StatusCodes.Status409Conflict)
            .Produces<ReceivingOrderCommandProblem>(StatusCodes.Status422UnprocessableEntity);

        return routeBuilder;
    }

    static async Task<IResult> SetInReceiving(
        [FromServices] ReceivingOrderCommandService service,
        [FromRoute] Guid id,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return TypedResults.Unauthorized();
        }

        var result = await service.StartReceivingFromAssignedLocationAsync(id, userId, ct);

        return CommandResult(result);
    }

    static async Task<IResult> SetReceived(
        [FromServices] ReceivingOrderCommandService service,
        [FromRoute] Guid id,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return TypedResults.Unauthorized();
        }

        var result = await service.SetReceivedAsync(id, userId, ct);

        return CommandResult(result);
    }

    static async Task<IResult> GetOrder(
        [FromServices] ReceivingOrderQueryService service,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var orderDetails = await service.GetOrderAsync(id, ct);

        if (orderDetails == null)
        {
            return Results.NotFound();
        }
        return Results.Ok(orderDetails);
    }

    private static IResult CommandResult(OperationResult result)
    {
        if (result.IsSuccess)
            return TypedResults.Ok();

        var error = result.Error!;
        var (statusCode, code) = error.Type switch
        {
            OperationErrorType.NotFound => (StatusCodes.Status404NotFound, "resource_not_found"),
            OperationErrorType.Conflict => (StatusCodes.Status409Conflict, "request_conflict"),
            OperationErrorType.Invalid => (StatusCodes.Status422UnprocessableEntity, "invalid_command"),
            _ => (StatusCodes.Status400BadRequest, "command_failed")
        };
        return Results.Json(
            new ReceivingOrderCommandProblem(code, error.Message),
            statusCode: statusCode);
    }

    private sealed record ReceivingOrderCommandProblem(string Code, string Message);
}
