using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.Payments.DTOs;
using Sanes.Web.Api;

namespace Sanes.Web.PaymentReceipts;

public sealed class PaymentReceiptsWebService
    : IPaymentReceiptsWebService
{
    private readonly ISanesApiClient _apiClient;

    public PaymentReceiptsWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PaymentReceiptListResponse> GetPagedAsync(
        PaymentReceiptListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        var parameters =
            new List<string>
            {
                $"page={request.Page}",
                $"pageSize={request.PageSize}"
            };

        if (request.From.HasValue)
        {
            parameters.Add(
                $"from={request.From.Value:yyyy-MM-dd}");
        }

        if (request.To.HasValue)
        {
            parameters.Add(
                $"to={request.To.Value:yyyy-MM-dd}");
        }

        if (!string.IsNullOrWhiteSpace(
                request.Search))
        {
            parameters.Add(
                "search=" +
                Uri.EscapeDataString(
                    request.Search.Trim()));
        }

        if (request.CollectorId.HasValue)
        {
            parameters.Add(
                $"collectorId={request.CollectorId.Value:D}");
        }

        var uri =
            "api/payment-receipts?" +
            string.Join(
                "&",
                parameters);

        using var message =
            new HttpRequestMessage(
                HttpMethod.Get,
                uri);

        using var response =
            await _apiClient.SendAsync(
                message,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(
                await ReadApiErrorAsync(
                    response,
                    cancellationToken));
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                PaymentReceiptListResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty payment receipt list response.");
    }

    public async Task<PaymentReceiptResponse?> GetByIdAsync(
        Guid receiptId,
        CancellationToken cancellationToken = default)
    {
        if (receiptId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del recibo no es válido.",
                nameof(receiptId));
        }

        using var message =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/payment-receipts/{receiptId:D}");

        using var response =
            await _apiClient.SendAsync(
                message,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                PaymentReceiptResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty payment receipt response.");
    }

    public async Task<PaymentReceiptResponse?> GetByNumberAsync(
        string receiptNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                receiptNumber))
        {
            throw new ArgumentException(
                "El número de recibo es requerido.",
                nameof(receiptNumber));
        }

        var normalized =
            receiptNumber
                .Trim();

        using var message =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/payment-receipts/by-number/" +
                Uri.EscapeDataString(
                    normalized));

        using var response =
            await _apiClient.SendAsync(
                message,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                PaymentReceiptResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty payment receipt response.");
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
            return
                "La consulta de recibos no es válida.";
        }

        try
        {
            using var document =
                JsonDocument.Parse(
                    content);

            var root =
                document.RootElement;

            if (root.TryGetProperty(
                    "message",
                    out var messageElement) &&
                messageElement.ValueKind ==
                    JsonValueKind.String)
            {
                var message =
                    messageElement.GetString();

                if (!string.IsNullOrWhiteSpace(
                        message))
                {
                    return TranslateApiMessage(
                        message);
                }
            }

            if (root.TryGetProperty(
                    "title",
                    out var titleElement) &&
                titleElement.ValueKind ==
                    JsonValueKind.String)
            {
                var title =
                    titleElement.GetString();

                if (!string.IsNullOrWhiteSpace(
                        title))
                {
                    return title;
                }
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
            "From date cannot be after To date." =>
                "La fecha desde no puede ser posterior a la fecha hasta.",

            "Page must be greater than zero." =>
                "La página debe ser mayor que cero.",

            "PageSize must be between 1 and 100." =>
                "El tamaño de página debe estar entre 1 y 100.",

            "Search cannot exceed 150 characters." =>
                "La búsqueda no puede exceder 150 caracteres.",

            "The requested page is outside the supported range." =>
                "La página solicitada está fuera del rango permitido.",

            _ =>
                message
        };
    }
}