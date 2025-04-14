﻿using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.MessageBus.Abstractions;
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

        group.MapPost("/kafka", CreateOrderToKafka).WithName("CreateOrderToKafka");
    }

    private static async Task<Results<Created<OrderIn>, ProblemHttpResult>> CreateOrder(
        [FromServices] IOrderInService orderService,
        [FromBody] OrderInCreateCommand createCommand)
    {
        var result = await orderService.CreateOrderAsync(createCommand);

        return TypedResults.Created($"/api/orders-in/{result.Id}", result);
    }

    private static async Task<Results<Ok, ProblemHttpResult>> CreateOrderToKafka(
        [FromServices] IOrderInCommandProducer producer,
        [FromBody] OrderInCreateCommand createCommand)
    {
        await producer.CreateOrderCommandProduce(createCommand);

        return TypedResults.Ok();
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

    private static async Task<Results<Ok<List<OrderIn>>, NotFound, ProblemHttpResult>> GetOrderList(
        [FromServices] IOrderInService orderService,
        [FromQuery] string? orderBy = null,
        [FromQuery] int? skip = null,
        [FromQuery] int? take = null)
    {
        var orderQuery = new OrderQuery(orderBy, skip, take);

        var result = await orderService.GetOrderListAsync(orderQuery);

        return result is List<OrderIn> model ? TypedResults.Ok(model) : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<OrderIn>, NotFound, ProblemHttpResult>> GetOrderById(
        [FromServices] IOrderInService orderService,
        [FromRoute] Guid id)
    {
        var result = await orderService.GetOrderByIdAsync(id);

        return result is OrderIn model ? TypedResults.Ok(model) : TypedResults.NotFound();
    }
}
