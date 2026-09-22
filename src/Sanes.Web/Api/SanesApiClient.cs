using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Sanes.Web.Authentication;

namespace Sanes.Web.Api;

public sealed class SanesApiClient
    : ISanesApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IWebSessionStore _sessionStore;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<SanesApiClient> _logger;

    public SanesApiClient(
        IHttpClientFactory httpClientFactory,
        AuthenticationStateProvider authenticationStateProvider,
        IWebSessionStore sessionStore,
        NavigationManager navigationManager,
        ILogger<SanesApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _authenticationStateProvider = authenticationStateProvider;
        _sessionStore = sessionStore;
        _navigationManager = navigationManager;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authenticationState =
            await _authenticationStateProvider
                .GetAuthenticationStateAsync();

        var principal =
            authenticationState.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            _navigationManager.NavigateTo(
                "/login",
                forceLoad: true);

            return new HttpResponseMessage(
                HttpStatusCode.Unauthorized);
        }

        var sessionId =
            principal.FindFirst(
                WebAuthConstants.SessionIdClaim)?
                .Value;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogWarning(
                "Authenticated Web user does not contain a session identifier.");

            _navigationManager.NavigateTo(
                "/login",
                forceLoad: true);

            return new HttpResponseMessage(
                HttpStatusCode.Unauthorized);
        }

        var session =
            await _sessionStore.GetAsync(
                sessionId,
                cancellationToken);

        if (session is null)
        {
            _logger.LogInformation(
                "Web session {SessionId} is no longer available.",
                sessionId);

            _navigationManager.NavigateTo(
                "/login?reason=session-expired",
                forceLoad: true);

            return new HttpResponseMessage(
                HttpStatusCode.Unauthorized);
        }

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                session.AccessToken);

        var client =
            _httpClientFactory.CreateClient(
                "SanesApi");

        var response =
            await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.Unauthorized)
        {
            await _sessionStore.RemoveAsync(
                sessionId,
                cancellationToken);

            _logger.LogInformation(
                "Sanes.Api rejected the current JWT. " +
                "The Web session will be invalidated.");

            _navigationManager.NavigateTo(
                "/login?reason=session-expired",
                forceLoad: true);

            return response;
        }

        if (response.StatusCode ==
            HttpStatusCode.Forbidden)
        {
            _logger.LogInformation(
                "Sanes.Api returned Forbidden for the current user.");

            _navigationManager.NavigateTo(
                "/forbidden",
                forceLoad: true);
        }

        return response;
    }
}