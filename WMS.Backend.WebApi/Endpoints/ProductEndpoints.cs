using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services.ProductServices;
using Dto = WMS.Shared.Models.Catalogs;

namespace WMS.Backend.WebApi.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("/api/Product")
            .WithTags(nameof(Dto.Product))
            .WithOpenApi()
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            //.RequireAuthorization()
            ;

        group.MapPost("/", CreateProduct).WithName("CreateProduct");
        group.MapPut("/{id}", UpdateProduct).WithName("UpdateProduct");
        group.MapDelete("/{id}", DeleteProduct).WithName("DeleteProduct");
        group.MapGet("/{id}", GetProduct).WithName("GetProduct");
        group.MapGet("/", GetListProduct).WithName("GetListProduct");
    }

    private static async Task<Results<Created<Dto.Product>, BadRequest<List<ValidationResult>>, ProblemHttpResult>> CreateProduct(
        HttpContext httpContext,
        [FromServices] IProductService productService,
        [FromBody] Dto.Product product)
    {
        var problemDetails = DtoValidator.Validate(product, httpContext);

        if (problemDetails != null) 
            return TypedResults.Problem(problemDetails);

        var result = await productService.CreateProductAsync(product);

        return TypedResults.Created($"/api/Product/{result.Id}", result);
    }

    private static async Task<Results<NoContent, NotFound<Dto.Product>>> UpdateProduct(
        [FromServices] IProductService productService,
        [FromRoute] Guid id,
        [FromBody] Dto.Product product)
    {
        var isSuccess = await productService.UpdateProductAsync(id, product);

        return isSuccess ? TypedResults.NoContent() : TypedResults.NotFound(product);
    }

    private static async Task<Results<NoContent, NotFound<Guid>>> DeleteProduct(
        [FromServices] IProductService productService,
        [FromRoute] Guid id)
    {
        var isSuccess = await productService.DeleteProductAsync(id);

        return isSuccess ? TypedResults.NoContent() : TypedResults.NotFound(id);
    }

    private static async Task<Results<Ok<Dto.Product>, NotFound<Guid>>> GetProduct(
    [FromServices] IProductService productService,
    [FromRoute] Guid id)
    {
        var result = await productService.GetProductAsync(id);

        return result is Dto.Product dto ? TypedResults.Ok(dto) : TypedResults.NotFound(id);
    }

    private static async Task<Results<Ok<List<Dto.Product>>, NotFound>> GetListProduct(
        [FromServices] IProductService productService,
        [FromQuery] string? orderBy,
        [FromQuery] int? skip = null,
        [FromQuery] int? take = null,
        [FromQuery] string? nameSubstring = null)
    {
        var productQuery = new ProductQuery(orderBy, skip, take, nameSubstring);

        var result = await productService.GetListProductAsync(productQuery);

        return result is List<Dto.Product> dto ? TypedResults.Ok(dto) : TypedResults.NotFound();
    }    
}
