namespace Wms.Application.ReceivingOrders;

public sealed record AddPutawayMovementCommand(Guid OrderId, int LineNumber, Guid DestinationStorageLocationId, decimal Quantity);
public sealed record UpdatePutawayMovementCommand(Guid OrderId, Guid MovementId, Guid DestinationStorageLocationId, decimal Quantity);
public sealed record DeletePutawayMovementCommand(Guid OrderId, Guid MovementId);
