using System.Net;
using System.Net.Http.Json;
using Sanes.Application.Loans.DTOs;
using Sanes.Web.Api;

namespace Sanes.Web.EarlySettlements;

public sealed class EarlySettlementsWebService
    : IEarlySettlementsWebService
{
    private readonly ISanesApiClient _apiClient;

    public EarlySettlementsWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<EarlySettlementQuoteResponse?> QuoteAsync(
        Guid loanId,
        EarlySettlementQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateLoanId(
            loanId);

        ArgumentNullException.ThrowIfNull(
            request);

        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"api/loans/{loanId:D}/early-settlement/quote")
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
                await ReadApiErrorAsync(
                    response,
                    cancellationToken));
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                EarlySettlementQuoteResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty early settlement quote response.");
    }

    public async Task<EarlySettlementResponse?> ExecuteAsync(
        Guid loanId,
        EarlySettlementExecuteRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateLoanId(
            loanId);

        ArgumentNullException.ThrowIfNull(
            request);

        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"api/loans/{loanId:D}/early-settlement")
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
                await ReadApiErrorAsync(
                    response,
                    cancellationToken));
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                EarlySettlementResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty early settlement response.");
    }

    public async Task<EarlySettlementResponse?> GetByLoanAsync(
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        ValidateLoanId(
            loanId);

        using var message =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/loans/{loanId:D}/early-settlement");

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
                EarlySettlementResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty early settlement response.");
    }

    private static void ValidateLoanId(
        Guid loanId)
    {
        if (loanId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del préstamo no es válido.",
                nameof(loanId));
        }
    }

    private static async Task<string> ReadApiErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var error =
                await response.Content
                    .ReadFromJsonAsync<ApiErrorResponse>(
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
            // Fallback a texto plano.
        }

        var content =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        if (string.IsNullOrWhiteSpace(
            content))
        {
            return
                "La operación de liquidación anticipada no es válida.";
        }

        return TranslateApiMessage(
            content.Trim('"'));
    }

    private static string TranslateApiMessage(
        string message)
    {
        const string minimumPrefix =
            "At least 6 completed installments are required for early settlement.";

        const string changedPrefix =
            "The settlement amount has changed.";

        if (message.StartsWith(
            minimumPrefix,
            StringComparison.Ordinal))
        {
            return
                "Se requieren al menos 6 cuotas completadas para realizar " +
                "una liquidación anticipada.";
        }

        if (message.StartsWith(
            changedPrefix,
            StringComparison.Ordinal))
        {
            return
                "El monto de la liquidación cambió desde la última " +
                "cotización. Solicita una nueva cotización antes de continuar.";
        }

        return message switch
        {
            "LoanId must be a valid identifier." =>
                "El identificador del préstamo no es válido.",

            "Cancelled loans cannot be settled early." =>
                "Un préstamo cancelado no puede liquidarse anticipadamente.",

            "The loan is already paid." =>
                "El préstamo ya se encuentra pagado.",

            "The loan has already been settled early." =>
                "Este préstamo ya fue liquidado anticipadamente.",

            "The loan has no contractual balance eligible for early settlement." =>
                "El préstamo no tiene saldo contractual disponible para una liquidación anticipada.",

            "Early settlement discount type is invalid." =>
                "El tipo de descuento seleccionado no es válido.",

            "Discount value must be greater than zero." =>
                "El valor del descuento debe ser mayor que cero.",

            "Installment waiver must be 1, 2, or 3 installments." =>
                "Solo se pueden perdonar 1, 2 o 3 cuotas.",

            "Percentage discount cannot exceed 100%." =>
                "El porcentaje de descuento no puede superar el 100%.",

            "The selected discount does not produce a monetary discount." =>
                "El descuento seleccionado no genera un beneficio monetario.",

            "Expected settlement amount cannot be negative." =>
                "El monto esperado de liquidación no puede ser negativo.",

            "A reason is required for early settlement." =>
                "Debes indicar el motivo de la liquidación anticipada.",

            "Early settlement reason cannot exceed 500 characters." =>
                "El motivo no puede exceder 500 caracteres.",

            "Early settlement did not fully clear the outstanding loan balance." =>
                "La liquidación no pudo cerrar completamente el saldo pendiente.",

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