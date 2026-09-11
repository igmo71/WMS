namespace Wms.Application.ShippingOrders;

public sealed record AddPickingMovementCommand(Guid OrderId, int LineNumber, Guid SourceStorageLocationId, decimal Quantity);
public sealed record UpdatePickingMovementCommand(Guid OrderId, Guid MovementId, Guid SourceStorageLocationId, decimal Quantity);
public sealed record DeletePickingMovementCommand(Guid OrderId, Guid MovementId);
