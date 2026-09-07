using System.Net;
using System.Net.Http.Json;
using Wms.Contracts.Mobile.V1;

namespace Wms.Mobile.Services;

public sealed class MobileInventoryTransferClient
{
    private readonly MobileApiTransport _transport;

    internal MobileInventoryTransferClient(MobileApiTransport transport)
    {
        _transport = transport;
    }

    public async Task<IReadOnlyList<MobileInventoryTransferSummaryResponse>> GetListAsync(
        Guid warehouseId,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.InventoryTransfers}?warehouseId={warehouseId:D}";
        using HttpResponseMessage response = await _transport.GetAsync(route, ct);
        return await response.Content
            .ReadFromJsonAsync<List<MobileInventoryTransferSummaryResponse>>(ct)
            ?? throw new MobileApiException(
                response.StatusCode,
                "invalid_inventory_transfers_response",
                "Сервер вернул некорректный список перемещений.");
    }

    public async Task<MobileInventoryTransferSummaryResponse> CreateAsync(
        Guid warehouseId,
        Guid clientRequestId,
        Guid? transitStorageLocationId = null,
        CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _transport.PostAsync(
            MobileApiRoutes.InventoryTransfers,
            new MobileCreateInventoryTransferRequest(
                clientRequestId,
                warehouseId,
                transitStorageLocationId),
            ct);
        return await MobileApiTransport.ReadCommandResponseAsync<MobileInventoryTransferSummaryResponse>(
            response,
            "Сервер вернул некорректное перемещение.",
            ct);
    }

    public async Task<MobileInventoryTransferSummaryResponse?> GetByTransitLocationAsync(
        Guid transitStorageLocationId,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.InventoryTransfers}/by-transit-location/"
            + transitStorageLocationId.ToString("D");
        using HttpResponseMessage response = await _transport.GetAsync(route, ct);
        if (response.StatusCode == HttpStatusCode.NoContent)
            return null;

        return await response.Content
            .ReadFromJsonAsync<MobileInventoryTransferSummaryResponse>(ct)
            ?? throw new MobileApiException(
                response.StatusCode,
                "invalid_inventory_transfer_response",
                "Сервер вернул некорректное перемещение.");
    }

    public async Task<MobileInventoryTransferDetailsResponse> GetAsync(
        Guid transferId,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.InventoryTransfers}/{transferId:D}";
        using HttpResponseMessage response = await _transport.GetAsync(route, ct);
        return await response.Content
            .ReadFromJsonAsync<MobileInventoryTransferDetailsResponse>(ct)
            ?? throw new MobileApiException(
                response.StatusCode,
                "invalid_inventory_transfer_details_response",
                "Сервер вернул некорректную историю перемещения.");
    }

    public async Task<MobileDirectTransferSkuResponse> ResolveDirectSkuAsync(
        Guid transferId,
        Guid sourceStorageLocationId,
        string barcode,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.InventoryTransfers}/{transferId:D}/direct/sku/resolve";
        using HttpResponseMessage response = await _transport.PostAsync(
            route,
            new MobileResolveDirectTransferSkuRequest(barcode, sourceStorageLocationId),
            ct);
        return await response.Content.ReadFromJsonAsync<MobileDirectTransferSkuResponse>(ct)
            ?? throw new MobileApiException(
                response.StatusCode,
                "invalid_direct_transfer_sku_response",
                "Сервер вернул некорректные сведения о товаре и остатке.");
    }

    public async Task<IReadOnlyList<MobileDirectTransferSkuSearchResponse>> SearchDirectSkusAsync(
        Guid transferId,
        Guid sourceStorageLocationId,
        string query,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.InventoryTransfers}/{transferId:D}/direct/skus"
            + $"?sourceStorageLocationId={sourceStorageLocationId:D}"
            + $"&query={Uri.EscapeDataString(query)}";
        using HttpResponseMessage response = await _transport.GetAsync(route, ct);
        return await response.Content
            .ReadFromJsonAsync<List<MobileDirectTransferSkuSearchResponse>>(ct)
            ?? throw new MobileApiException(
                response.StatusCode,
                "invalid_direct_transfer_sku_search_response",
                "Сервер вернул некорректные результаты поиска товара.");
    }

    public async Task<MobileDirectTransferSkuResponse> ResolveTransitSkuAsync(
        Guid transferId,
        string barcode,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.InventoryTransfers}/{transferId:D}/transit/sku/resolve";
        using HttpResponseMessage response = await _transport.PostAsync(
            route,
            new MobileResolveTransitTransferSkuRequest(barcode),
            ct);
        return await response.Content.ReadFromJsonAsync<MobileDirectTransferSkuResponse>(ct)
            ?? throw new MobileApiException(
                response.StatusCode,
                "invalid_transit_transfer_sku_response",
                "Сервер вернул некорректные сведения о товаре в транзитной ячейке.");
    }

    public async Task<IReadOnlyList<MobileDirectTransferSkuSearchResponse>> SearchTransitSkusAsync(
        Guid transferId,
        string query,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.InventoryTransfers}/{transferId:D}/transit/skus"
            + $"?query={Uri.EscapeDataString(query)}";
        using HttpResponseMessage response = await _transport.GetAsync(route, ct);
        return await response.Content
            .ReadFromJsonAsync<List<MobileDirectTransferSkuSearchResponse>>(ct)
            ?? throw new MobileApiException(
                response.StatusCode,
                "invalid_transit_transfer_sku_search_response",
                "Сервер вернул некорректные результаты поиска товара.");
    }

    public async Task<MobileMoveDirectInventoryTransferResponse> MoveDirectAsync(
        Guid transferId,
        Guid sourceStorageLocationId,
        Guid destinationStorageLocationId,
        Guid stockKeepingUnitId,
        decimal quantity,
        Guid clientRequestId,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.InventoryTransfers}/{transferId:D}/direct-movements";
        using HttpResponseMessage response = await _transport.PostAsync(
            route,
            new MobileMoveDirectInventoryTransferRequest(
                clientRequestId,
                sourceStorageLocationId,
                destinationStorageLocationId,
                stockKeepingUnitId,
                quantity),
            ct);
        return await MobileApiTransport.ReadCommandResponseAsync<MobileMoveDirectInventoryTransferResponse>(
            response,
            "Сервер вернул некорректный результат перемещения.",
            ct);
    }

    public async Task<MobileTransitInventoryTransferMovementResponse> PickToTransitAsync(
        Guid transferId,
        Guid sourceStorageLocationId,
        Guid stockKeepingUnitId,
        decimal quantity,
        Guid clientRequestId,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.InventoryTransfers}/{transferId:D}/pick-to-transit";
        using HttpResponseMessage response = await _transport.PostAsync(
            route,
            new MobilePickToTransitRequest(
                clientRequestId,
                sourceStorageLocationId,
                stockKeepingUnitId,
                quantity),
            ct);
        return await MobileApiTransport.ReadCommandResponseAsync<MobileTransitInventoryTransferMovementResponse>(
            response,
            "Сервер вернул некорректный результат перемещения в транзитную ячейку.",
            ct);
    }

    public async Task<MobileTransitInventoryTransferMovementResponse> PutFromTransitAsync(
        Guid transferId,
        Guid destinationStorageLocationId,
        Guid stockKeepingUnitId,
        decimal quantity,
        Guid clientRequestId,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.InventoryTransfers}/{transferId:D}/put-from-transit";
        using HttpResponseMessage response = await _transport.PostAsync(
            route,
            new MobilePutFromTransitRequest(
                clientRequestId,
                destinationStorageLocationId,
                stockKeepingUnitId,
                quantity),
            ct);
        return await MobileApiTransport.ReadCommandResponseAsync<MobileTransitInventoryTransferMovementResponse>(
            response,
            "Сервер вернул некорректный результат перемещения из транзитной ячейки.",
            ct);
    }

    public async Task<MobileCompleteInventoryTransferResponse> CompleteAsync(
        Guid transferId,
        Guid clientRequestId,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.InventoryTransfers}/{transferId:D}/complete";
        using HttpResponseMessage response = await _transport.PostAsync(
            route,
            new MobileCompleteInventoryTransferRequest(clientRequestId),
            ct);
        return await MobileApiTransport.ReadCommandResponseAsync<MobileCompleteInventoryTransferResponse>(
            response,
            "Сервер вернул некорректный результат завершения перемещения.",
            ct);
    }
}
