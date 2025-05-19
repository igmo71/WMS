using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services.OrderInServices;
using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.WebApi.Endpoints;

public static class OrderInEndpoints
{
    public static void MapOrderInEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/OrderIn")
            .WithTags(nameof(Dto.OrderIn))
            .WithOpenApi()
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            //.RequireAuthorization()
            ;

        group.MapPost("/", CreateOrderIn).WithName("CreateOrderIn");
        group.MapPut("/{id}", UpdateOrderIn).WithName("UpdateOrderIn");
        group.MapDelete("/{id}", DeleteOrderIn).WithName("DeleteOrderIn");
        group.MapGet("/{id}", GetOrderIn).WithName("GetOrderIn");
        group.MapGet("/", GetListOrderIn).WithName("GetListOrderIn");
    }

    private static async Task<Created<Dto.OrderIn>> CreateOrderIn(
        [FromServices] IOrderInService orderService,
        [FromBody] Dto.OrderIn orderDto)
    {
        var result = await orderService.CreateOrderInAsync(orderDto);

        return TypedResults.Created($"/api/OrderIn/{result.Id}", result);
    }

    private static async Task<Results<NoContent, NotFound>> UpdateOrderIn(
        [FromServices] IOrderInService orderService,
        [FromRoute] Guid id,
        [FromBody] Dto.OrderIn orderDto)
    {
        await orderService.UpdateOrderInAsync(id, orderDto);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> DeleteOrderIn(
        [FromServices] IOrderInService orderService,
        [FromRoute] Guid id)
    {
        await orderService.DeleteOrderInAsync(id);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<Dto.OrderIn>, NotFound>> GetOrderIn(
        [FromServices] IOrderInService orderService,
        [FromRoute] Guid id)
    {
        var result = await orderService.GetOrderAsync(id);

        return result is Dto.OrderIn dto ? TypedResults.Ok(dto) : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<List<Dto.OrderIn>>, NotFound>> GetListOrderIn(
        [FromServices] IOrderInService orderService,
        [FromQuery] string? orderBy = null,
        [FromQuery] int? skip = null,
        [FromQuery] int? take = null,
        [FromQuery] DateTime? dateBegin = null,
        [FromQuery] DateTime? dateEnd = null,
        [FromQuery] string? numberSubstring = null)
    {
        var orderQuery = new OrderInGetListQuery(orderBy, skip, take, dateBegin, dateEnd, numberSubstring);

        var result = await orderService.GetListOrderInAsync(orderQuery);

        return result is List<Dto.OrderIn> dto ? TypedResults.Ok(dto) : TypedResults.NotFound();
    }
}
