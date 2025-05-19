using Microsoft.EntityFrameworkCore;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Services.ProductServices;
using WMS.Backend.Domain.Models.Catalogs;
using WMS.Backend.Infrastructure.Data;

namespace WMS.Backend.Infrastructure.Repositories
{
    internal class ProductRepository(AppDbContext dbContext) : IProductRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<Product> CreateAsync(Product product)
        {
            var result = _dbContext.Products.Add(product).Entity;

            await _dbContext.SaveChangesAsync();

            return result;
        }

        public async Task<bool> UpdateAsync(Guid id, Product product)
        {
            var affected = await _dbContext.Products
                .Where(model => model.Id == id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.Id, product.Id)
                    .SetProperty(m => m.Name, product.Name));

            return affected == 1;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var affected = await _dbContext.Products
                .Where(e => e.Id == id)
                .ExecuteDeleteAsync();

            return affected == 1;
        }

        public async Task<Product?> GetAsync(Guid id)
        {
            var result = await _dbContext.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            return result;
        }

        public async Task<List<Product>> GetListAsync(ProductQuery productQuery)
        {
            var result = await _dbContext.Products
                .AsNoTracking()
                .HandleQuery(productQuery)
                .ToListAsync();

            return result;
        }
    }
}
