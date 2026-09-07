using Microsoft.EntityFrameworkCore;
using Wms.Application.Persistence;
using Wms.Common;
using Wms.Data;
using Wms.Domain;

namespace Wms.Application.Warehouses;

public class WarehouseService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
{
    public async Task<OperationResult> CreateOrUpdateAsync(
        Warehouse item,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var existing = await dbContext.Warehouses
            .FirstOrDefaultAsync(x => x.Id == item.Id, ct);

        if (existing is null)
        {
            dbContext.Warehouses.Add(item);
        }
        else
        {
            var activityChanged = existing.DeletionMark != item.DeletionMark;
            existing.Name = item.Name;
            existing.DeletionMark = item.DeletionMark;

            if (activityChanged)
            {
                var locations = await dbContext.StorageLocations
                    .Where(x => x.WarehouseId == existing.Id)
                    .ToListAsync(ct);

                foreach (var location in locations)
                {
                    location.AdvanceOperationalRevision();
                }
            }
        }

        return await ApplicationPersistence.SaveChangesAsync(dbContext, ct);
    }

    public async Task<Warehouse?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var result = await dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return result;
    }

    public async Task<ListResult<Warehouse>> ListAsync(ListQuery listQuery, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Warehouse> query = dbContext.Warehouses
                .AsNoTracking();

        if (listQuery.ExcludeDeleted)
            query = query.Where(x => x.DeletionMark == false);

        query = ApplySearch(query, listQuery.SearchString);

        int totalItems = await query.CountAsync(ct);

        query = ApplySorting(query, listQuery.SortBy, listQuery.SortDescending);

        var items = await query
            .Skip(listQuery.Skip)
            .Take(listQuery.Take)
            .ToListAsync(ct);

        return new ListResult<Warehouse>
        {
            Items = items,
            TotalItems = totalItems
        };
    }

    private static IQueryable<Warehouse> ApplySearch(IQueryable<Warehouse> query, string? searchString)
    {
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(x => x.Name!.Contains(searchString));
        }

        return query;
    }

    private static IQueryable<Warehouse> ApplySorting(IQueryable<Warehouse> query, string? sortBy, bool sortDescending)
    {
        return sortBy switch
        {
            "Name" => sortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            _ => query.OrderByDescending(x => x.Name),
        };
    }
}
