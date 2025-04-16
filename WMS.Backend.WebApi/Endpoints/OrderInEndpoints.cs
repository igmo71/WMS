using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.WebApi.Endpoints;

public static class OrderInEndpoints
{
    public static void MapOrderInEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/orders-in")
            .WithTags(nameof(OrderIn))
            .WithOpenApi()
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/", CreateOrder).WithName("CreateOrderIn");
        group.MapPut("/{id}", UpdateOrder).WithName("UpdateOrderIn");
        group.MapDelete("/{id}", DeleteOrder).WithName("DeleteOrderIn");
        group.MapGet("/", GetOrderList).WithName("GetOrderInList");
        group.MapGet("/{id}", GetOrderById).WithName("GetOrderInById");
    }

    private static async Task<Created<OrderIn>> CreateOrder(
        [FromServices] IOrderInService orderService,
        [FromBody] OrderInCreateCommand createCommand)
    {
        var result = await orderService.CreateOrderAsync(createCommand);

        return TypedResults.Created($"/api/orders-in/{result.Id}", result);
    }

    private static async Task<Results<NoContent, NotFound>> UpdateOrder(
        [FromServices] IOrderInService orderService,
        [FromRoute] Guid id,
        [FromBody] OrderIn order)
    {
        await orderService.UpdateOrderAsync(id, order);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> DeleteOrder(
        [FromServices] IOrderInService orderService,
        [FromRoute] Guid id)
    {
        await orderService.DeleteOrderAsync(id);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<List<OrderIn>>, NotFound>> GetOrderList(
        [FromServices] IOrderInService orderService,
        [FromQuery] string? orderBy = null,
        [FromQuery] int? skip = null,
        [FromQuery] int? take = null,
        [FromQuery] DateTime? dateBegin = null,
        [FromQuery] DateTime? dateEnd = null,
        [FromQuery] string? numberSubstring = null)
    {
        var orderQuery = new OrderInGetListQuery(orderBy, skip, take, dateBegin, dateEnd, numberSubstring);

        var result = await orderService.GetOrderListAsync(orderQuery);

        return result is List<OrderIn> model ? TypedResults.Ok(model) : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<OrderIn>, NotFound>> GetOrderById(
        [FromServices] IOrderInService orderService,
        [FromRoute] Guid id)
    {
        var result = await orderService.GetOrderByIdAsync(id);

        return result is OrderIn model ? TypedResults.Ok(model) : TypedResults.NotFound();
    }
}
