using System.Net.Http.Json;
using Wms.Contracts.Mobile.V1;

namespace Wms.Mobile.Services;

public sealed class MobileIdentityClient
{
    private readonly MobileApiTransport _transport;
    private readonly IMobileSessionStore _sessionStore;

    internal MobileIdentityClient(
        MobileApiTransport transport,
        IMobileSessionStore sessionStore)
    {
        _transport = transport;
        _sessionStore = sessionStore;
    }

    public async Task<MobileCurrentUserResponse> LoginAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _transport.PostAsync(
            MobileApiRoutes.Login,
            new MobileLoginRequest(email, password),
            ct);
        MobileSessionResponse tokenResponse = await response.Content
            .ReadFromJsonAsync<MobileSessionResponse>(ct)
            ?? throw new MobileApiException(
                response.StatusCode,
                "invalid_session_response",
                "Сервер вернул некорректный ответ сессии.");

        await _sessionStore.SaveAsync(MobileAuthenticationHandler.ToSession(tokenResponse));
        return await GetCurrentUserAsync(ct);
    }

    public async Task<MobileCurrentUserResponse> GetCurrentUserAsync(
        CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _transport.GetAsync(MobileApiRoutes.Me, ct);
        return await response.Content.ReadFromJsonAsync<MobileCurrentUserResponse>(ct)
            ?? throw new MobileApiException(
                response.StatusCode,
                "invalid_current_user_response",
                "Сервер вернул некорректные сведения о пользователе.");
    }

    public void Logout() => _sessionStore.Clear();
}
