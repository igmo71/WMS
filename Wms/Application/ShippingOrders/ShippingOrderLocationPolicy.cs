using Microsoft.EntityFrameworkCore;
using Wms.Application.StorageLocations;
using Wms.Common;
using Wms.Data;
using Wms.Domain;
using Wms.Domain.Enums;

namespace Wms.Application.ShippingOrders;

internal static class ShippingOrderLocationPolicy
{
    public static async Task<OperationResult> RequireShippingLocationAsync(
        ApplicationDbContext dbContext,
        ShippingOrder order,
        Guid? shippingLocationId,
        CancellationToken ct)
    {
        if (shippingLocationId is not Guid locationId)
        {
            return OperationError.Invalid("Для отгрузки не указана позиция отгрузки.");
        }

        var location = await dbContext.StorageLocations
            .Include(x => x.Warehouse)
            .Include(x => x.Zone)
            .Include(x => x.ActiveLock)
            .SingleOrDefaultAsync(x => x.Id == locationId, ct);

        if (location is null
            || !IsActiveLocation(location, order.WarehouseId, ZoneType.Shipping))
        {
            return OperationError.Invalid(
                "Для отгрузки требуется активная позиция зоны отгрузки склада ордера.");
        }

        return StorageLocationAvailability.ValidateUnlocked(location);
    }

    public static Task<OperationResult> ValidatePickingRoutesAsync(
        ApplicationDbContext dbContext,
        ShippingOrder order,
        IReadOnlyCollection<InventoryMovement> movements,
        CancellationToken ct) =>
        ValidateRoutesAsync(
            dbContext,
            order.WarehouseId,
            movements,
            ZoneType.Storage,
            ZoneType.Shipping,
            "Каждое движение отбора должно сохранять маршрут из активной позиции хранения в активную позицию отгрузки склада ордера.",
            ct);

    public static Task<OperationResult> ValidateRollbackRoutesAsync(
        ApplicationDbContext dbContext,
        ShippingOrder order,
        IReadOnlyCollection<InventoryMovement> movements,
        CancellationToken ct) =>
        ValidateRoutesAsync(
            dbContext,
            order.WarehouseId,
            movements,
            ZoneType.Shipping,
            ZoneType.Storage,
            "Компенсация отбора требует активного маршрута из позиции отгрузки в позицию хранения склада ордера.",
            ct);

    private static async Task<OperationResult> ValidateRoutesAsync(
        ApplicationDbContext dbContext,
        Guid warehouseId,
        IReadOnlyCollection<InventoryMovement> movements,
        ZoneType sourceZoneType,
        ZoneType destinationZoneType,
        string errorMessage,
        CancellationToken ct)
    {
        var locationIds = movements
            .SelectMany(x => new[]
            {
                x.SourceStorageLocationId,
                x.DestinationStorageLocationId
            })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();

        var locations = await dbContext.StorageLocations
            .Include(x => x.Warehouse)
            .Include(x => x.Zone)
            .Include(x => x.ActiveLock)
            .Where(x => locationIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        foreach (var movement in movements)
        {
            if (movement.SourceStorageLocationId is not Guid sourceId
                || movement.DestinationStorageLocationId is not Guid destinationId
                || !locations.TryGetValue(sourceId, out var source)
                || !locations.TryGetValue(destinationId, out var destination)
                || !IsActiveLocation(source, warehouseId, sourceZoneType)
                || !IsActiveLocation(destination, warehouseId, destinationZoneType))
            {
                return OperationError.Invalid(errorMessage);
            }
        }

        return OperationResult.Success();
    }

    private static bool IsActiveLocation(
        StorageLocation location,
        Guid warehouseId,
        ZoneType zoneType) =>
        location.WarehouseId == warehouseId
        && location.Warehouse is { DeletionMark: false }
        && location.Zone is { DeletionMark: false }
        && location.Zone.Type == zoneType
        && !location.IsFolder
        && !location.DeletionMark;
}
