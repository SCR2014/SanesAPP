using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Sanes.Application.Authentication.DTOs;

namespace Sanes.Web.Authentication;

public sealed class WebAuthenticationService
    : IWebAuthenticationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebSessionStore _sessionStore;
    private readonly ILogger<WebAuthenticationService> _logger;

    public WebAuthenticationService(
        IHttpClientFactory httpClientFactory,
        IWebSessionStore sessionStore,
        ILogger<WebAuthenticationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    public async Task<WebAuthenticationResult> SignInAsync(
        LoginRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(httpContext);

        var client =
            _httpClientFactory.CreateClient("SanesApi");

        HttpResponseMessage response;

        try
        {
            response =
                await client.PostAsJsonAsync(
                    "api/auth/login",
                    request,
                    cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Unable to connect to Sanes.Api during login.");

            return WebAuthenticationResult.Failure(
                "No fue posible conectar con el servicio de autenticación.");
        }
        catch (TaskCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                ex,
                "Sanes.Api login request timed out.");

            return WebAuthenticationResult.Failure(
                "El servicio de autenticación no respondió a tiempo.");
        }

        using (response)
        {
            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                return WebAuthenticationResult.Failure(
                    "Tenant, usuario o contraseña incorrectos.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Sanes.Api login returned status code {StatusCode}.",
                    (int)response.StatusCode);

                return WebAuthenticationResult.Failure(
                    "No fue posible iniciar sesión.");
            }

            var authResponse =
                await response.Content
                    .ReadFromJsonAsync<AuthResponse>(
                        cancellationToken);

            if (authResponse is null ||
                string.IsNullOrWhiteSpace(
                    authResponse.AccessToken))
            {
                _logger.LogError(
                    "Sanes.Api returned an invalid authentication response.");

                return WebAuthenticationResult.Failure(
                    "La respuesta de autenticación no es válida.");
            }

            var expiresAtUtc =
                NormalizeUtc(
                    authResponse.ExpiresAt);

            if (expiresAtUtc <= DateTimeOffset.UtcNow)
            {
                _logger.LogWarning(
                    "Sanes.Api returned an already expired token.");

                return WebAuthenticationResult.Failure(
                    "La sesión recibida ya expiró.");
            }

            var sessionId =
                Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(32));

            var session =
                new WebSession
                {
                    AccessToken =
                        authResponse.AccessToken,

                    ExpiresAtUtc =
                        expiresAtUtc
                };

            await _sessionStore.SetAsync(
                sessionId,
                session,
                cancellationToken);

            var claims =
                new List<Claim>
                {
                    new(
                        ClaimTypes.NameIdentifier,
                        authResponse.AppUserId.ToString("D")),

                    new(
                        ClaimTypes.Name,
                        authResponse.Name),

                    new(
                        ClaimTypes.Role,
                        authResponse.Role.ToString()),

                    new(
                        "tenant_id",
                        authResponse.TenantId.ToString("D")),

                    new(
                        "username",
                        authResponse.Username),

                    new(
                        WebAuthConstants.SessionIdClaim,
                        sessionId)
                };

            var identity =
                new ClaimsIdentity(
                    claims,
                    WebAuthConstants.AuthenticationScheme);

            var principal =
                new ClaimsPrincipal(identity);

            var properties =
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = false,
                    ExpiresUtc = expiresAtUtc
                };

            try
            {
                await httpContext.SignInAsync(
                    WebAuthConstants.AuthenticationScheme,
                    principal,
                    properties);
            }
            catch
            {
                await _sessionStore.RemoveAsync(
                    sessionId,
                    cancellationToken);

                throw;
            }

            return WebAuthenticationResult.Success(
                authResponse.Role);
        }
    }

    private static DateTimeOffset NormalizeUtc(
        DateTime value)
    {
        var utc =
            value.Kind switch
            {
                DateTimeKind.Utc =>
                    value,

                DateTimeKind.Local =>
                    value.ToUniversalTime(),

                _ =>
                    DateTime.SpecifyKind(
                        value,
                        DateTimeKind.Utc)
            };

        return new DateTimeOffset(utc);
    }
}