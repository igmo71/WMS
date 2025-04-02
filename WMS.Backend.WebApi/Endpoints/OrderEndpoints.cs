using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services;
using WMS.Backend.Domain.Models;

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
        [FromBody] Order order)
    {
        var result = await orderService.CreateAsync(order);

        return TypedResults.Created($"/api/orders/{result.Id}", result);
    }

    private static async Task<Results<Ok, NotFound>> UpdateOrder(
        [FromServices] IOrderService orderService,
        [FromRoute] Guid id,
        [FromBody] Order order)
    {
        var isSuccess = await orderService.UpdateAsync(id, order);

        return isSuccess ? TypedResults.Ok() : TypedResults.NotFound();
    }

    private static async Task<Results<Ok, NotFound>> DeleteOrder(
        [FromServices] IOrderService orderService,
        [FromRoute] Guid id)
    {
        var isSuccess = await orderService.DeleteAsync(id);

        return isSuccess ? TypedResults.Ok() : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<List<Order>>, NotFound, ProblemHttpResult>> GetOrderList(
        [FromServices] IOrderService orderService,
        [FromQuery] int? skip = null,
        [FromQuery] int? take = null)
    {
        var orderQuery = new OrderQuery(skip, take);

        var result = await orderService.GetListAsync(orderQuery);

        return result is List<Order> model ? TypedResults.Ok(model) : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<Order>, NotFound, ProblemHttpResult>> GetOrderById(
        [FromServices] IOrderService orderService,
        [FromRoute] Guid id)
    {
        var result = await orderService.GetByIdAsync(id);

        return result is Order model ? TypedResults.Ok(model) : TypedResults.NotFound();
    }
}
