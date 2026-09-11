namespace Wms.Application.ShippingOrders;

public sealed record RollbackShippingOrderCommand(Guid OrderId, string Reason);
