using System.Net;
using System.Net.Http.Json;
using Sanes.Application.LateFees.DTOs;
using Sanes.Web.Api;

namespace Sanes.Web.LateFees;

public sealed class LateFeeManagementWebService
    : ILateFeeManagementWebService
{
    private readonly ISanesApiClient _apiClient;

    public LateFeeManagementWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<LateFeeLoanResponse?> GetByLoanAsync(
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        ValidateLoanId(
            loanId);

        using var message =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/late-fees/loans/{loanId:D}");

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
            .ReadFromJsonAsync<LateFeeLoanResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty late fee response.");
    }

    public async Task<LateFeeChargeResponse?> AddAdjustmentAsync(
        Guid chargeId,
        LateFeeAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateChargeId(
            chargeId);

        ArgumentNullException.ThrowIfNull(
            request);

        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"api/late-fees/charges/{chargeId:D}/adjustments")
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
            .ReadFromJsonAsync<LateFeeChargeResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty late fee charge response.");
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

    private static void ValidateChargeId(
        Guid chargeId)
    {
        if (chargeId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del cargo de mora no es válido.",
                nameof(chargeId));
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
                "La operación de mora no pudo completarse.";
        }

        return TranslateApiMessage(
            content.Trim('"'));
    }

    private static string TranslateApiMessage(
        string message)
    {
        const string outstandingPrefix =
            "Adjustment amount cannot exceed the outstanding late fee balance of ";

        if (message.StartsWith(
            outstandingPrefix,
            StringComparison.Ordinal))
        {
            var amount =
                message[
                    outstandingPrefix.Length..]
                    .Trim()
                    .TrimEnd('.');

            return
                "El monto del ajuste no puede superar " +
                $"el saldo pendiente de mora de {amount}.";
        }

        return message switch
        {
            "LoanId must be a valid identifier." =>
                "El identificador del préstamo no es válido.",

            "Late fee charge id must be a valid identifier." =>
                "El identificador del cargo de mora no es válido.",

            "Loan not found." =>
                "El préstamo no fue encontrado.",

            "Late fee charge not found." =>
                "El cargo de mora no fue encontrado.",

            "Loan associated with the late fee charge was not found." =>
                "No fue posible encontrar el préstamo asociado al cargo de mora.",

            "Late fee adjustment type is invalid." =>
                "El tipo de ajuste de mora no es válido.",

            "Adjustment amount must be greater than zero." =>
                "El monto del ajuste debe ser mayor que cero.",

            "Adjustment reason is required." =>
                "Debes indicar el motivo del ajuste.",

            "Adjustment reason cannot exceed 500 characters." =>
                "El motivo del ajuste no puede exceder 500 caracteres.",

            "This late fee charge has no outstanding balance to reduce or waive." =>
                "Este cargo de mora no tiene saldo pendiente que pueda disminuirse o perdonarse.",

            "Only fixed late fees are currently supported." =>
                "Actualmente solo se admite mora de monto fijo por cuota.",

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