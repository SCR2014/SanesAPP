using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.Tenants.DTOs;
using Sanes.Web.Api;

namespace Sanes.Web.TenantSettings;

public sealed class TenantSettingsWebService
    : ITenantSettingsWebService
{
    private readonly ISanesApiClient _apiClient;

    public TenantSettingsWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<TenantDto?> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/tenants/me");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<TenantDto>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api devolvió una respuesta vacía al consultar la configuración del negocio.");
    }

    public async Task<TenantDto?> UpdateCurrentAsync(
        UpdateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        using var message =
            new HttpRequestMessage(
                HttpMethod.Put,
                "api/tenants/me")
            {
                Content =
                    JsonContent.Create(
                        request)
            };

        using var response =
            await _apiClient.SendAsync(
                message,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(
                TranslateApiMessage(
                    await ReadApiErrorAsync(
                        response,
                        cancellationToken)));
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<TenantDto>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api devolvió una respuesta vacía al actualizar la configuración del negocio.");
    }

    private static async Task<string> ReadApiErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        if (string.IsNullOrWhiteSpace(
                content))
        {
            return "La configuración proporcionada no es válida.";
        }

        try
        {
            using var document =
                JsonDocument.Parse(
                    content);

            var root =
                document.RootElement;

            if (root.ValueKind ==
                JsonValueKind.String)
            {
                return root.GetString()
                    ?? "La configuración proporcionada no es válida.";
            }

            if (root.TryGetProperty(
                    "message",
                    out var message) &&
                message.ValueKind ==
                    JsonValueKind.String)
            {
                return message.GetString()
                    ?? "La configuración proporcionada no es válida.";
            }

            if (root.TryGetProperty(
                    "title",
                    out var title) &&
                title.ValueKind ==
                    JsonValueKind.String)
            {
                return title.GetString()
                    ?? "La configuración proporcionada no es válida.";
            }
        }
        catch (JsonException)
        {
            return content.Trim('"');
        }

        return content.Trim('"');
    }

    private static string TranslateApiMessage(
        string message)
    {
        return message switch
        {
            "Name cannot be empty or contain only spaces." =>
                "El nombre comercial no puede estar vacío.",

            "CurrencyCode cannot be empty or contain only spaces." =>
                "El código de moneda no puede estar vacío.",

            "CurrencySymbol cannot be empty or contain only spaces." =>
                "El símbolo de moneda no puede estar vacío.",

            "DefaultLateFeeCalculationType is invalid." =>
                "El tipo de cálculo de mora no es válido.",

            "DefaultLateFeeAmount cannot be negative." =>
                "El monto de mora no puede ser negativo.",

            "DefaultLateFeeGraceDays cannot be negative." =>
                "Los días de gracia no pueden ser negativos.",

            "DefaultLateFeeAmount must be greater than zero when late fees are enabled." =>
                "El monto de mora debe ser mayor que cero cuando la mora está habilitada.",

            "GuaranteeRequiredFromAmount must be greater than zero when configured." =>
                "El monto mínimo para exigir garantía debe ser mayor que cero.",

            "Late fee calculation type is invalid." =>
                "El tipo de cálculo de mora no es válido.",

            "Only fixed late fees are currently supported." =>
                "Actualmente solo se admite mora de monto fijo por cuota vencida.",

            "Late fee amount cannot be negative." =>
                "El monto de mora no puede ser negativo.",

            "Late fee grace days cannot be negative." =>
                "Los días de gracia no pueden ser negativos.",

            "Late fee amount must be greater than zero when late fees are enabled." =>
                "El monto de mora debe ser mayor que cero cuando la mora está habilitada.",

            "Guarantee required from amount must be greater than zero when configured." =>
                "El monto mínimo para exigir garantía debe ser mayor que cero.",

            _ =>
                message
        };
    }
}