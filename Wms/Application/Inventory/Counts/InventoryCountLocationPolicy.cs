using Wms.Common;
using Wms.Domain;
using Wms.Domain.Enums;

namespace Wms.Application.Inventory.Counts;

internal static class InventoryCountLocationPolicy
{
    public static OperationResult RequireActiveStorageLocation(
        StorageLocation location,
        Guid warehouseId) =>
        location.WarehouseId == warehouseId
        && location.Warehouse is { DeletionMark: false }
        && location.Zone is { DeletionMark: false, Type: ZoneType.Storage }
        && !location.IsFolder
        && !location.DeletionMark
            ? OperationResult.Success()
            : OperationError.Invalid(
                "Инвентаризация доступна только для активной ячейки зоны хранения выбранного склада.");
}
