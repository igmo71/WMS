using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using WMS.Backend.Domain.Models;
using WMS.Backend.Infrastructure.Data;
namespace WMS.Backend.WebApi.Endpoints;

public static class OrderEndpointsBase
{
    public static void MapOrderEndpointsBase(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/orders").WithTags(nameof(Order));

        group.MapGet("/", async (AppDbContext db) =>
        {
            return await db.Orders.ToListAsync();
        })
        .WithName("GetAllOrders")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<Order>, NotFound>> (Guid id, AppDbContext db) =>
        {
            return await db.Orders.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Id == id)
                is Order model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetOrderById")
        .WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (Guid id, Order order, AppDbContext db) =>
        {
            var affected = await db.Orders
                .Where(model => model.Id == id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.Id, order.Id)
                    .SetProperty(m => m.Name, order.Name)
                    .SetProperty(m => m.Number, order.Number)
                    .SetProperty(m => m.DateTime, order.DateTime)
                    );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateOrder")
        .WithOpenApi();

        group.MapPost("/", async (Order order, AppDbContext db) =>
        {
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/orders/{order.Id}", order);
        })
        .WithName("CreateOrder")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (Guid id, AppDbContext db) =>
        {
            var affected = await db.Orders
                .Where(model => model.Id == id)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteOrder")
        .WithOpenApi();
    }
}
