using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.WebApi.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/orders")
            .WithTags(nameof(Order))
            .WithOpenApi()
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/", CreateOrder).WithName("CreateOrder");

        group.MapPut("/{id}", UpdateOrder).WithName("UpdateOrder");

        group.MapDelete("/{id}", DeleteOrder).WithName("DeleteOrder");

        group.MapGet("/", GetOrderList).WithName("GetOrderList");

        group.MapGet("/{id}", GetOrderById).WithName("GetOrderById");
    }

    private static async Task<Results<Created<Order>, ProblemHttpResult>> CreateOrder(
        [FromServices] IOrderService orderService,
        [FromBody] CreateOrderCommand createCommand)
    {
        var result = await orderService.CreateOrderAsync(createCommand);

        return TypedResults.Created($"/api/orders/{result.Id}", result);
    }

    private static async Task<Results<Ok, NotFound>> UpdateOrder(
        [FromServices] IOrderService orderService,
        [FromRoute] Guid id,
        [FromBody] Order order)
    {
        var isSuccess = await orderService.UpdateOrderAsync(id, order);

        return isSuccess ? TypedResults.Ok() : TypedResults.NotFound();
    }

    private static async Task<Results<Ok, NotFound>> DeleteOrder(
        [FromServices] IOrderService orderService,
        [FromRoute] Guid id)
    {
        var isSuccess = await orderService.DeleteOrderAsync(id);

        return isSuccess ? TypedResults.Ok() : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<List<Order>>, NotFound, ProblemHttpResult>> GetOrderList(
        [FromServices] IOrderService orderService,
        [FromQuery] string? orderBy = null,
        [FromQuery] int? skip = null,
        [FromQuery] int? take = null)
    {
        var orderQuery = new OrderQuery(orderBy, skip, take);

        var result = await orderService.GetOrderListAsync(orderQuery);

        return result is List<Order> model ? TypedResults.Ok(model) : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<Order>, NotFound, ProblemHttpResult>> GetOrderById(
        [FromServices] IOrderService orderService,
        [FromRoute] Guid id)
    {
        var result = await orderService.GetOrderByIdAsync(id);

        return result is Order model ? TypedResults.Ok(model) : TypedResults.NotFound();
    }
}
