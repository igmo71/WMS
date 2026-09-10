namespace Wms.Application.Inventory.Transfers;

public sealed record CreateInventoryTransferCommand(Guid WarehouseId, Guid? TransitStorageLocationId);

public sealed record MoveDirectInventoryTransferCommand(
    Guid TransferId, Guid SourceStorageLocationId, Guid DestinationStorageLocationId,
    Guid StockKeepingUnitId, decimal Quantity);

public sealed record PickInventoryTransferCommand(
    Guid TransferId, Guid SourceStorageLocationId, Guid StockKeepingUnitId, decimal Quantity);

public sealed record PutInventoryTransferCommand(
    Guid TransferId, Guid DestinationStorageLocationId, Guid StockKeepingUnitId, decimal Quantity);
