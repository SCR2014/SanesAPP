using System.Net;
using System.Net.Http.Json;
using Sanes.Application.Payments.DTOs;
using Sanes.Web.Api;
using Sanes.Application.Payments.Exceptions;

namespace Sanes.Web.Payments;

public sealed class PaymentsWebService
    : IPaymentsWebService
{
    private readonly ISanesApiClient _apiClient;

    public PaymentsWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<PaymentResponse>>
        GetByLoanAsync(
            Guid loanId,
            CancellationToken cancellationToken = default)
    {
        if (loanId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del préstamo no es válido.",
                nameof(loanId));
        }

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/payments?loanId={loanId:D}");

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
            .ReadFromJsonAsync<
                List<PaymentResponse>>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty payments response.");
    }

    public async Task<PaymentResponse?> GetByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        if (paymentId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del pago no es válido.",
                nameof(paymentId));
        }

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/payments/{paymentId:D}");

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
            .ReadFromJsonAsync<PaymentResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty payment response.");
    }

    public async Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (idempotencyKey == Guid.Empty)
        {
            throw new ArgumentException(
                "La clave de idempotencia no es válida.",
                nameof(idempotencyKey));
        }

        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/payments")
            {
                Content =
                    JsonContent.Create(
                        request)
            };

        message.Headers.TryAddWithoutValidation(
            "Idempotency-Key",
            idempotencyKey.ToString("D"));

        using var response =
            await _apiClient.SendAsync(
                message,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.Conflict)
        {
            throw new PaymentIdempotencyConflictException(
                await ReadApiErrorAsync(
                    response,
                    cancellationToken));
        }

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
            .ReadFromJsonAsync<PaymentResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty payment response.");
    }

    public async Task<PaymentReversalResponse>
        ReverseAsync(
            Guid paymentId,
            PaymentReversalRequest request,
            CancellationToken cancellationToken = default)
    {
        if (paymentId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del pago no es válido.",
                nameof(paymentId));
        }

        ArgumentNullException.ThrowIfNull(
            request);

        if (string.IsNullOrWhiteSpace(
                request.Reason))
        {
            throw new ArgumentException(
                "El motivo del reverso es requerido.",
                nameof(request));
        }

        var reason =
            request.Reason.Trim();

        if (reason.Length > 500)
        {
            throw new ArgumentException(
                "El motivo del reverso no puede exceder 500 caracteres.",
                nameof(request));
        }

        var normalizedRequest =
            new PaymentReversalRequest
            {
                Reason =
                    reason
            };

        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"api/payments/{paymentId:D}/reversal")
            {
                Content =
                    JsonContent.Create(
                        normalizedRequest)
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
            .ReadFromJsonAsync<
                PaymentReversalResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty payment reversal response.");
    }

    private static async Task<string>
        ReadApiErrorAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
    {
        try
        {
            var error =
                await response.Content
                    .ReadFromJsonAsync<
                        ApiErrorResponse>(
                            cancellationToken);

            if (!string.IsNullOrWhiteSpace(
                error?.Message))
            {
                return TranslateApiMessage(
                    error.Message);
            }

            if (error?.Errors is not null)
            {
                var validationError =
                    error.Errors
                        .SelectMany(
                            x => x.Value)
                        .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(
                    validationError))
                {
                    return TranslateApiMessage(
                        validationError);
                }
            }

            if (!string.IsNullOrWhiteSpace(
                error?.Title))
            {
                return error.Title;
            }
        }
        catch
        {
            // Fallback to plain text below.
        }

        var content =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        if (string.IsNullOrWhiteSpace(
            content))
        {
            return
                "La operación de pago solicitada no es válida.";
        }

        return TranslateApiMessage(
            content.Trim('"'));
    }

    private static string TranslateApiMessage(
        string message)
    {
        const string exceedBalancePrefix =
            "Payment amount cannot exceed the total outstanding balance of ";

        if (message.StartsWith(
            exceedBalancePrefix,
            StringComparison.Ordinal))
        {
            var balance =
                message[
                    exceedBalancePrefix.Length..];

            return
                "El monto del pago no puede exceder " +
                $"el saldo total pendiente de {balance}";
        }

        return message switch
        {
            "LoanId must be a valid identifier." =>
                "El identificador del préstamo no es válido.",

            "Loan not found or does not belong to the specified tenant." =>
                "El préstamo no fue encontrado.",

            "Payments can only be registered for active loans." =>
                "Solo se pueden registrar pagos en préstamos activos.",

            "The client associated with the loan could not be found." =>
                "No fue posible encontrar el cliente asociado al préstamo.",

            "This loan has no outstanding balance." =>
                "Este préstamo no tiene saldo pendiente.",

            "A regular payment must apply at least one installment amount to the loan balance." =>
                "Un pago regular debe aplicar al menos el monto de una cuota al saldo del préstamo.",

            "A partial payment cannot settle the entire outstanding balance." =>
                "Un pago parcial no puede liquidar todo el saldo pendiente.",

            "A partial payment must apply less than one installment amount to the loan balance." =>
                "Un pago parcial debe aplicar menos del monto de una cuota al saldo del préstamo.",

            "A full settlement payment must equal the total outstanding balance." =>
                "Un pago total debe ser exactamente igual al saldo total pendiente.",

            "Invalid payment type." =>
                "El tipo de pago no es válido.",

            "PaymentType is invalid." =>
                "El tipo de pago no es válido.",

            "PaymentDate is required." =>
                "La fecha del pago es requerida.",

            "Payment not found or does not belong to the specified tenant." =>
                "El pago no fue encontrado.",

            "The payment has already been reversed." =>
                "El pago ya fue reversado.",

            "Only the latest effective payment of the loan can be reversed." =>
                "Solo se puede reversar el último pago vigente del préstamo.",

            "Payments generated by an early settlement cannot be reversed from the payment reversal module." =>
                "Los pagos generados por una liquidación anticipada no pueden reversarse desde este módulo.",

            "The loan associated with the payment could not be found." =>
                "No fue posible encontrar el préstamo asociado al pago.",

            "A reversal reason is required." =>
                "El motivo del reverso es requerido.",

            "Reversal reason is required." =>
                "El motivo del reverso es requerido.",

            "Reversal reason cannot exceed 500 characters." =>
                "El motivo del reverso no puede exceder 500 caracteres.",

            _ =>
                message
        };
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }

        public string? Title { get; set; }

        public Dictionary<string, string[]>?
            Errors { get; set; }
    }
}