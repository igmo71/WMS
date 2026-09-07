namespace Wms.Contracts.Mobile.V1;

public enum MobileInventoryTransferStatus
{
    Draft = 0,
    InProgress = 1,
    Completed = 2
}

public sealed record MobileInventoryTransferSummaryResponse(
    Guid Id,
    string Number,
    DateTime Date,
    Guid WarehouseId,
    string WarehouseName,
    MobileInventoryTransferStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    MobileStorageLocationResponse? TransitStorageLocation);

public sealed record MobileInventoryTransferMovementResponse(
    Guid MovementId,
    Guid StockKeepingUnitId,
    string SkuCode,
    string SkuName,
    string? UnitOfMeasure,
    decimal Quantity,
    MobileInventoryMovementLocationResponse Source,
    MobileInventoryMovementLocationResponse Destination);

public sealed record MobileInventoryTransferDetailsResponse(
    MobileInventoryTransferSummaryResponse Transfer,
    IReadOnlyList<MobileInventoryTransferMovementResponse> Movements,
    IReadOnlyList<MobileInventoryTransferSkuBalanceResponse> TransitBalances);

public sealed record MobileInventoryTransferSkuBalanceResponse(
    Guid StockKeepingUnitId,
    string SkuCode,
    string SkuName,
    string? UnitOfMeasure,
    decimal Quantity);

public sealed record MobileCreateInventoryTransferRequest(
    Guid ClientRequestId,
    Guid WarehouseId,
    Guid? TransitStorageLocationId = null);

public sealed record MobileResolveDirectTransferSkuRequest(
    string Barcode,
    Guid SourceStorageLocationId);

public sealed record MobileDirectTransferSkuResponse(
    Guid Id,
    string Code,
    string Name,
    string? UnitOfMeasure,
    decimal AvailableQuantity);

public sealed record MobileDirectTransferSkuSearchResponse(
    Guid Id,
    string Code,
    string Name,
    string? UnitOfMeasure,
    decimal AvailableQuantity,
    bool IsExactMatch);

public sealed record MobileResolveTransitTransferSkuRequest(string Barcode);

public sealed record MobilePickToTransitRequest(
    Guid ClientRequestId,
    Guid SourceStorageLocationId,
    Guid StockKeepingUnitId,
    decimal Quantity);

public sealed record MobilePutFromTransitRequest(
    Guid ClientRequestId,
    Guid DestinationStorageLocationId,
    Guid StockKeepingUnitId,
    decimal Quantity);

public sealed record MobileTransitInventoryTransferMovementResponse(
    Guid MovementId,
    Guid TransferId,
    MobileInventoryTransferStatus TransferStatus);

public sealed record MobileMoveDirectInventoryTransferRequest(
    Guid ClientRequestId,
    Guid SourceStorageLocationId,
    Guid DestinationStorageLocationId,
    Guid StockKeepingUnitId,
    decimal Quantity);

public sealed record MobileInventoryMovementLocationResponse(
    Guid Id,
    string Address,
    string Name);

public sealed record MobileMoveDirectInventoryTransferResponse(
    Guid MovementId,
    Guid TransferId,
    int LineNumber,
    Guid StockKeepingUnitId,
    string SkuCode,
    string SkuName,
    string? UnitOfMeasure,
    decimal Quantity,
    MobileInventoryMovementLocationResponse Source,
    MobileInventoryMovementLocationResponse Destination,
    DateTimeOffset PostedAtUtc,
    MobileInventoryTransferStatus TransferStatus);

public sealed record MobileCompleteInventoryTransferRequest(Guid ClientRequestId);

public sealed record MobileCompleteInventoryTransferResponse(
    Guid TransferId,
    MobileInventoryTransferStatus Status,
    DateTimeOffset CompletedAtUtc);

