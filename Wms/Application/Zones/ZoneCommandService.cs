using Microsoft.EntityFrameworkCore;
using Wms.Application.Persistence;
using Wms.Application.Zones;
using Wms.Common;
using Wms.Data;
using Wms.Domain;

namespace Wms.Application.Zones;

public class ZoneCommandService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
{
    public async Task<OperationResult<Zone>> SaveAsync(
        SaveZoneCommand command,
        CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        Zone? zone = await FindForUpdateAsync(dbContext, command.Id, ct);

        if (command.Id.HasValue && zone is null)
        {
            return OperationError.NotFound($"Зона '{command.Id}' не найдена.");
        }

        Guid? originalWarehouseId = zone?.WarehouseId;
        var originalType = zone?.Type;

        OperationResult<Zone> domainResult = ApplyCommand(zone, command);
        if (!domainResult.IsSuccess)
        {
            return domainResult.Error!;
        }

        zone = domainResult.Value!;
        if (!command.Id.HasValue)
        {
            dbContext.Zones.Add(zone);
        }

        OperationResult stateValidation = await ValidateStateAsync(
            dbContext,
            zone,
            originalWarehouseId,
            ct);

        if (!stateValidation.IsSuccess)
        {
            return stateValidation.Error!;
        }

        if (originalType.HasValue
            && (originalType.Value != zone.Type || originalWarehouseId != zone.WarehouseId))
        {
            await AdvanceLocationRevisionsAsync(dbContext, zone.Id, ct);
        }

        var saveResult = await ApplicationPersistence.SaveChangesAsync(dbContext, ct);
        return saveResult.IsSuccess ? zone : saveResult.Error!;
    }

    public async Task<OperationResult> MarkDeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        Zone? zone = await dbContext.Zones.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (zone is null)
        {
            return OperationError.NotFound($"Зона '{id}' не найдена.");
        }

        if (!zone.DeletionMark)
        {
            zone.Deactivate();
            await AdvanceLocationRevisionsAsync(dbContext, zone.Id, ct);
        }

        return await ApplicationPersistence.SaveChangesAsync(dbContext, ct);
    }

    public async Task<OperationResult> UnMarkDeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        Zone? zone = await dbContext.Zones
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (zone is null)
        {
            return OperationError.NotFound($"Зона '{id}' не найдена.");
        }

        if (zone.Warehouse is null || zone.Warehouse.DeletionMark)
        {
            return OperationError.Invalid("Зону можно активировать только в активном складе.");
        }

        if (zone.DeletionMark)
        {
            zone.Activate();
            await AdvanceLocationRevisionsAsync(dbContext, zone.Id, ct);
        }

        return await ApplicationPersistence.SaveChangesAsync(dbContext, ct);
    }

    private static Task<Zone?> FindForUpdateAsync(
        ApplicationDbContext dbContext,
        Guid? id,
        CancellationToken ct)
    {
        return id.HasValue
            ? dbContext.Zones.FirstOrDefaultAsync(x => x.Id == id.Value, ct)
            : Task.FromResult<Zone?>(null);
    }

    private static OperationResult<Zone> ApplyCommand(Zone? zone, SaveZoneCommand command)
    {
        if (zone is null)
        {
            return Zone.Create(
                Guid.NewGuid(),
                command.WarehouseId,
                command.Code,
                command.Name,
                command.Type);
        }

        OperationResult warehouseResult = zone.MoveToWarehouse(command.WarehouseId);
        if (!warehouseResult.IsSuccess)
        {
            return warehouseResult.Error!;
        }

        OperationResult detailsResult = zone.UpdateDetails(command.Code, command.Name, command.Type);
        if (!detailsResult.IsSuccess)
        {
            return detailsResult.Error!;
        }

        return zone;
    }

    private static async Task<OperationResult> ValidateStateAsync(
        ApplicationDbContext dbContext,
        Zone zone,
        Guid? originalWarehouseId,
        CancellationToken ct)
    {
        var warehouseIsActive = await dbContext.Warehouses.AnyAsync(
            x => x.Id == zone.WarehouseId && !x.DeletionMark,
            ct);
        if (!warehouseIsActive)
        {
            return OperationError.Invalid("Зона должна принадлежать активному складу.");
        }

        var codeIsUsed = await dbContext.Zones.AnyAsync(
            x => x.WarehouseId == zone.WarehouseId
                && x.Code == zone.Code
                && x.Id != zone.Id,
            ct);

        if (codeIsUsed)
        {
            return OperationError.Conflict("В выбранном складе уже есть зона с таким кодом.");
        }

        var changesWarehouse = originalWarehouseId.HasValue
            && originalWarehouseId.Value != zone.WarehouseId;

        if (changesWarehouse
            && await dbContext.StorageLocations.AnyAsync(x => x.ZoneId == zone.Id, ct))
        {
            return OperationError.Invalid(
                "Зону со складскими позициями нельзя перенести в другой склад.");
        }

        return OperationResult.Success();
    }

    private static async Task AdvanceLocationRevisionsAsync(
        ApplicationDbContext dbContext,
        Guid zoneId,
        CancellationToken ct)
    {
        var locations = await dbContext.StorageLocations
            .Where(x => x.ZoneId == zoneId)
            .ToListAsync(ct);

        foreach (var location in locations)
        {
            location.AdvanceOperationalRevision();
        }
    }

}
