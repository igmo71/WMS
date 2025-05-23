﻿using WMS.Backend.Application.Services.ProductServices;
using WMS.Backend.Domain.Models.Catalogs;

namespace WMS.Backend.Application.Abstractions.Services
{
    public interface IProductService
    {
        Task<Product> CreateProductAsync(Product newProduct);
        Task<bool> UpdateProductAsync(Guid id, Product product);
        Task<bool> DeleteProductAsync(Guid id);
        Task<List<Product>> GetProductListAsync(ProductQuery productQuery);
        Task<Product?> GetProductByIdAsync(Guid id);
    }
}
