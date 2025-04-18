using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services.ProductServices;
using WMS.Backend.Domain.Models.Catalogs;

namespace WMS.Backend.WebApi.Endpoints
{
    public static class ProductEndpoints
    {
        public static void MapProductEndpoints(this IEndpointRouteBuilder routeBuilder)
        {
            var routeGroup = routeBuilder.MapGroup("/api/products")
                .WithTags(nameof(Product))
                .WithOpenApi()
                .ProducesValidationProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            routeGroup.MapPost("/", CreateProduct).WithName("CreateProduct");

            routeGroup.MapPut("/{id}", UpdateProduct).WithName("UpdateProduct");

            routeGroup.MapDelete("/{id}", DeleteProduct).WithName("DeleteProduct");

            routeGroup.MapGet("/", GetProductList).WithName("GetProductList");

            routeGroup.MapGet("/{id}", GetProductById).WithName("GetProductById");
        }

        private static async Task<Results<Created<Product>, ProblemHttpResult>> CreateProduct(
            [FromServices] IProductService productService,
            [FromBody] Product product)
        {
            var result = await productService.CreateProductAsync(product);

            return TypedResults.Created($"/api/products/{result.Id}", result);
        }

        private static async Task<Results<Ok, NotFound>> UpdateProduct(
            [FromServices] IProductService productService,
            [FromRoute] Guid id,
            [FromBody] Product product)
        {
            var isSuccess = await productService.UpdateProductAsync(id, product);

            return isSuccess ? TypedResults.Ok() : TypedResults.NotFound();
        }

        private static async Task<Results<Ok, NotFound>> DeleteProduct(
            [FromServices] IProductService productService,
            [FromRoute] Guid id)
        {
            var isSuccess = await productService.DeleteProductAsync(id);

            return isSuccess ? TypedResults.Ok() : TypedResults.NotFound();
        }

        private static async Task<Results<Ok<List<Product>>, NotFound, ProblemHttpResult>> GetProductList(
            [FromServices] IProductService productService,
            [FromQuery] string? orderBy,
            [FromQuery] int? skip = null,
            [FromQuery] int? take = null,
            [FromQuery] string? nameSubstring = null)
        {
            var productQuery = new ProductQuery(orderBy, skip, take, nameSubstring);

            var result = await productService.GetProductListAsync(productQuery);

            return result is List<Product> model ? TypedResults.Ok(model) : TypedResults.NotFound();
        }

        private static async Task<Results<Ok<Product>, NotFound, ProblemHttpResult>> GetProductById(
        [FromServices] IProductService productService,
        [FromRoute] Guid id)
        {
            var result = await productService.GetProductByIdAsync(id);

            return result is Product model ? TypedResults.Ok(model) : TypedResults.NotFound();
        }
    }
}
