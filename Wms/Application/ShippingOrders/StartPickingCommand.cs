namespace Wms.Application.ShippingOrders;

public sealed record StartPickingCommand(Guid OrderId, Guid ShippingLocationId);
