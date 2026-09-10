namespace Wms.Application.Inventory.Counts;

public sealed record StartInventoryCountCommand(Guid WarehouseId, Guid StorageLocationId);
public sealed record IncrementInventoryCountSkuCommand(Guid InventoryCountId, string? Barcode);
public sealed record SetInventoryCountQuantityCommand(Guid InventoryCountId, Guid ItemId, decimal CountedQuantity);
public sealed record SetInventoryCountSkuQuantityCommand(Guid InventoryCountId, Guid StockKeepingUnitId, decimal CountedQuantity);
public sealed record RemoveInventoryCountItemCommand(Guid InventoryCountId, Guid ItemId);
