using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Wms.Contracts.Mobile.V1;

namespace Wms.Mobile.Services;

internal sealed class MobileApiTransport(HttpClient httpClient)
{
    public async Task<HttpResponseMessage> GetAsync(
        string route,
        CancellationToken ct = default)
    {
        HttpResponseMessage response = await httpClient.GetAsync(route, ct);
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                throw await CreateApiExceptionAsync(response, ct);
            }
            finally
            {
                response.Dispose();
            }
        }

        return response;
    }

    public async Task<HttpResponseMessage> PostAsync<TRequest>(
        string route,
        TRequest request,
        CancellationToken ct = default)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(route, request, ct);
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                throw await CreateApiExceptionAsync(response, ct);
            }
            finally
            {
                response.Dispose();
            }
        }

        return response;
    }

    public static async Task<TResponse> ReadCommandResponseAsync<TResponse>(
        HttpResponseMessage response,
        string message,
        CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<TResponse>(ct)
                ?? throw InvalidResponse(response, message);
        }
        catch (Exception exception) when (
            exception is JsonException or NotSupportedException or IOException)
        {
            throw new HttpRequestException(message, exception, response.StatusCode);
        }
    }

    public static HttpRequestException InvalidResponse(
        HttpResponseMessage response,
        string message) => new(message, null, response.StatusCode);

    private static async Task<Exception> CreateApiExceptionAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        MobileProblemResponse? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<MobileProblemResponse>(ct);
        }
        catch (Exception exception) when (
            exception is JsonException or NotSupportedException)
        {
            // The fallback below deliberately avoids exposing a raw server response.
        }

        if (response.StatusCode == HttpStatusCode.RequestTimeout
            || (int)response.StatusCode >= 500)
        {
            return new HttpRequestException(
                problem?.Message ?? "Сервер WMS не подтвердил результат операции.",
                null,
                response.StatusCode);
        }

        return new MobileApiException(
            response.StatusCode,
            problem?.Code ?? "request_failed",
            problem?.Message ?? "Не удалось выполнить запрос к WMS.");
    }
}
