using System.Net.Http.Json;
using Wms.Contracts.Mobile.V1;

namespace Wms.Mobile.Services;

public sealed class MobileShippingOrderClient
{
    private readonly MobileApiTransport _transport;

    internal MobileShippingOrderClient(MobileApiTransport transport)
    {
        _transport = transport;
    }

    public async Task<MobileShippingOrderWorkQueueResponse> GetWorkQueueAsync(
        Guid warehouseId,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.ShippingOrders}?warehouseId={warehouseId:D}";
        using HttpResponseMessage response = await _transport.GetAsync(route, ct);
        return await response.Content.ReadFromJsonAsync<MobileShippingOrderWorkQueueResponse>(ct)
            ?? throw MobileApiTransport.InvalidResponse(
                response,
                "Сервер вернул некорректную очередь отбора и отгрузки.");
    }

    public Task<MobileShippingOrderDetailsResponse> GetAsync(
        Guid orderId,
        CancellationToken ct = default) =>
        GetDetailsAsync($"{MobileApiRoutes.ShippingOrders}/{orderId:D}", ct);

    public async Task<MobileShippingOrderDetailsResponse> ResolveDocumentAsync(
        Guid warehouseId,
        string barcode,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.ShippingOrders}/resolve-document";
        using HttpResponseMessage response = await _transport.PostAsync(
            route,
            new MobileResolveShippingOrderDocumentRequest(warehouseId, barcode),
            ct);
        return await response.Content.ReadFromJsonAsync<MobileShippingOrderDetailsResponse>(ct)
            ?? throw MobileApiTransport.InvalidResponse(
                response,
                "Сервер вернул некорректный расходный ордер.");
    }

    public async Task<IReadOnlyList<MobileShippingOrderLineCandidateResponse>> ResolveSkuAsync(
        Guid orderId,
        string barcode,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.ShippingOrders}/{orderId:D}/lines/resolve-sku";
        using HttpResponseMessage response = await _transport.PostAsync(
            route,
            new MobileResolveShippingOrderSkuRequest(barcode),
            ct);
        return await response.Content
            .ReadFromJsonAsync<List<MobileShippingOrderLineCandidateResponse>>(ct)
            ?? throw MobileApiTransport.InvalidResponse(
                response,
                "Сервер вернул некорректные строки товара расходного ордера.");
    }

    public async Task<MobileShippingOrderLineSearchResponse> SearchLinesAsync(
        Guid orderId,
        string query,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.ShippingOrders}/{orderId:D}/lines/search"
            + $"?query={Uri.EscapeDataString(query)}";
        using HttpResponseMessage response = await _transport.GetAsync(route, ct);
        return await response.Content.ReadFromJsonAsync<MobileShippingOrderLineSearchResponse>(ct)
            ?? throw MobileApiTransport.InvalidResponse(
                response,
                "Сервер вернул некорректные результаты поиска строк расходного ордера.");
    }

    public async Task<IReadOnlyList<MobileShippingOrderSourceAvailabilityResponse>> GetSourcesAsync(
        Guid orderId,
        int lineNumber,
        CancellationToken ct = default)
    {
        var route = $"{MobileApiRoutes.ShippingOrders}/{orderId:D}"
            + $"/lines/{lineNumber}/sources";
        using HttpResponseMessage response = await _transport.GetAsync(route, ct);
        return await response.Content
            .ReadFromJsonAsync<List<MobileShippingOrderSourceAvailabilityResponse>>(ct)
            ?? throw MobileApiTransport.InvalidResponse(
                response,
                "Сервер вернул некорректные позиции отбора.");
    }

    public Task<MobileShippingOrderCommandResponse> StartPickingAsync(
        Guid orderId,
        string shippingLocationBarcode,
        Guid clientRequestId,
        CancellationToken ct = default) =>
        PostCommandAsync(
            $"{MobileApiRoutes.ShippingOrders}/{orderId:D}/start-picking",
            new MobileStartShippingOrderPickingRequest(
                clientRequestId,
                shippingLocationBarcode),
            ct);

    public Task<MobileShippingOrderCommandResponse> AddPickingMovementAsync(
        Guid orderId,
        int lineNumber,
        string sourceStorageLocationBarcode,
        decimal quantity,
        Guid clientRequestId,
        CancellationToken ct = default) =>
        PostCommandAsync(
            $"{MobileApiRoutes.ShippingOrders}/{orderId:D}/picking-movements",
            new MobileAddShippingOrderPickingMovementRequest(
                clientRequestId,
                lineNumber,
                sourceStorageLocationBarcode,
                quantity),
            ct);

    public Task<MobileShippingOrderCommandResponse> DeletePickingMovementAsync(
        Guid orderId,
        Guid movementId,
        Guid clientRequestId,
        CancellationToken ct = default) =>
        PostCommandAsync(
            $"{MobileApiRoutes.ShippingOrders}/{orderId:D}"
                + $"/picking-movements/{movementId:D}/delete",
            new MobileShippingOrderCommandRequest(clientRequestId),
            ct);

    public Task<MobileShippingOrderCommandResponse> CompletePickingAsync(
        Guid orderId,
        Guid clientRequestId,
        CancellationToken ct = default) =>
        PostCommandAsync(
            $"{MobileApiRoutes.ShippingOrders}/{orderId:D}/complete-picking",
            new MobileShippingOrderCommandRequest(clientRequestId),
            ct);

    public Task<MobileShippingOrderCommandResponse> ShipAsync(
        Guid orderId,
        Guid clientRequestId,
        CancellationToken ct = default) =>
        PostCommandAsync(
            $"{MobileApiRoutes.ShippingOrders}/{orderId:D}/ship",
            new MobileShippingOrderCommandRequest(clientRequestId),
            ct);

    private async Task<MobileShippingOrderDetailsResponse> GetDetailsAsync(
        string route,
        CancellationToken ct)
    {
        using HttpResponseMessage response = await _transport.GetAsync(route, ct);
        return await response.Content.ReadFromJsonAsync<MobileShippingOrderDetailsResponse>(ct)
            ?? throw MobileApiTransport.InvalidResponse(
                response,
                "Сервер вернул некорректный расходный ордер.");
    }

    private async Task<MobileShippingOrderCommandResponse> PostCommandAsync<TRequest>(
        string route,
        TRequest request,
        CancellationToken ct)
    {
        using HttpResponseMessage response = await _transport.PostAsync(route, request, ct);
        return await MobileApiTransport.ReadCommandResponseAsync<MobileShippingOrderCommandResponse>(
            response,
            "Сервер вернул некорректный результат операции с расходным ордером.",
            ct);
    }
}
