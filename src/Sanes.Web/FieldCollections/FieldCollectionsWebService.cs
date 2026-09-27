using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.FieldCollections.DTOs;
using Sanes.Application.Payments.DTOs;
using Sanes.Web.Api;

namespace Sanes.Web.FieldCollections;

public sealed class FieldCollectionsWebService
    : IFieldCollectionsWebService
{
    private readonly ISanesApiClient _apiClient;

    public FieldCollectionsWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<FieldCollectionDailyResponse> GetDailyAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/field-collections/daily" +
                $"?date={date:yyyy-MM-dd}");

        using var response =
            await _apiClient.SendAsync(
                request,
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
            .ReadFromJsonAsync<FieldCollectionDailyResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty field collection response.");
    }

    public async Task<FieldCollectionPaymentResponse> CreatePaymentAsync(
        CreateFieldCollectionPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/field-collections/payments")
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
            HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(
                await ReadApiErrorAsync(
                    response,
                    cancellationToken));
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<FieldCollectionPaymentResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty field payment response.");
    }

    public async Task<PaymentReceiptResponse?> GetReceiptByPaymentAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/payments/{paymentId:D}/receipt");

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
            .ReadFromJsonAsync<PaymentReceiptResponse>(
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
            return "La operación solicitada no es válida.";
        }

        try
        {
            using var document =
                JsonDocument.Parse(
                    content);

            var root =
                document.RootElement;

            string? message = null;

            if (root.ValueKind ==
                JsonValueKind.String)
            {
                message =
                    root.GetString();
            }
            else if (
                root.TryGetProperty(
                    "message",
                    out var messageElement) &&
                messageElement.ValueKind ==
                    JsonValueKind.String)
            {
                message =
                    messageElement.GetString();
            }
            else if (
                root.TryGetProperty(
                    "title",
                    out var titleElement) &&
                titleElement.ValueKind ==
                    JsonValueKind.String)
            {
                message =
                    titleElement.GetString();
            }

            if (!string.IsNullOrWhiteSpace(
                message))
            {
                return TranslateApiMessage(
                    message);
            }
        }
        catch (JsonException)
        {
            return TranslateApiMessage(
                content.Trim('"'));
        }

        return TranslateApiMessage(
            content.Trim('"'));
    }

    private static string TranslateApiMessage(
        string message)
    {
        if (message.StartsWith(
                "Payment amount cannot exceed the total outstanding balance of",
                StringComparison.OrdinalIgnoreCase))
        {
            var amount =
                message
                    .Replace(
                        "Payment amount cannot exceed the total outstanding balance of",
                        string.Empty,
                        StringComparison.OrdinalIgnoreCase)
                    .Trim()
                    .TrimEnd('.');

            return
                $"El monto del pago no puede exceder " +
                $"el saldo total pendiente de {amount}.";
        }

        return message switch
        {
            "This loan has no outstanding balance." =>
                "Este préstamo no tiene saldo pendiente.",

            "A regular payment must apply at least one installment amount to the loan balance." =>
                "Un pago regular debe aplicar al menos el monto de una cuota al saldo del préstamo.",

            "A partial payment cannot settle the entire outstanding balance." =>
                "Un pago parcial no puede liquidar todo el saldo pendiente.",

            "A partial payment must apply less than one installment amount to the loan balance." =>
                "Un pago parcial debe aplicar al saldo del préstamo menos del monto de una cuota.",

            "A full settlement payment must equal the total outstanding balance." =>
                "El pago total debe ser exactamente igual al saldo total pendiente.",

            "Invalid payment type." =>
                "El tipo de pago no es válido.",

            "Collector not found, inactive, or does not belong to the specified tenant." =>
                "El cobrador no existe, está inactivo o no pertenece a la empresa actual.",

            "Collection route not found, inactive, or does not belong to the specified tenant." =>
                "La ruta de cobranza no existe, está inactiva o no pertenece a la empresa actual.",

            "The loan client does not belong to the specified collection route." =>
                "El cliente del préstamo no pertenece a la ruta de cobranza indicada.",

            _ =>
                message
        };
    }
}