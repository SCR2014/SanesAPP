using System.ComponentModel.DataAnnotations;
using Sanes.Application.Loans.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Web.EarlySettlements;

public sealed class EarlySettlementFormModel
    : IValidatableObject
{
    public Guid LoanId { get; set; }

    public EarlySettlementDiscountType DiscountType { get; set; }
        = EarlySettlementDiscountType.InstallmentWaiver;

    [Range(
        typeof(decimal),
        "0.01",
        "1000000",
        ErrorMessage =
            "El valor del descuento debe ser mayor que cero.")]
    public decimal DiscountValue { get; set; } = 1m;

    [MaxLength(
        500,
        ErrorMessage =
            "El motivo no puede exceder 500 caracteres.")]
    public string? Reason { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (LoanId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Debes seleccionar un préstamo.",
                [nameof(LoanId)]);
        }

        if (!Enum.IsDefined(
            typeof(EarlySettlementDiscountType),
            DiscountType))
        {
            yield return new ValidationResult(
                "El tipo de descuento no es válido.",
                [nameof(DiscountType)]);
        }

        if (DiscountValue <= 0)
        {
            yield return new ValidationResult(
                "El valor del descuento debe ser mayor que cero.",
                [nameof(DiscountValue)]);

            yield break;
        }

        if (DiscountType ==
            EarlySettlementDiscountType.InstallmentWaiver)
        {
            if (DiscountValue != 1m &&
                DiscountValue != 2m &&
                DiscountValue != 3m)
            {
                yield return new ValidationResult(
                    "Solo se pueden perdonar 1, 2 o 3 cuotas.",
                    [nameof(DiscountValue)]);
            }
        }

        if (DiscountType ==
            EarlySettlementDiscountType.PercentageDiscount &&
            DiscountValue > 100m)
        {
            yield return new ValidationResult(
                "El porcentaje de descuento no puede superar el 100%.",
                [nameof(DiscountValue)]);
        }
    }

    public EarlySettlementQuoteRequest ToQuoteRequest()
    {
        return new EarlySettlementQuoteRequest
        {
            DiscountType =
                DiscountType,

            DiscountValue =
                DiscountValue
        };
    }

    public EarlySettlementExecuteRequest ToExecuteRequest(
        EarlySettlementQuoteResponse quote)
    {
        ArgumentNullException.ThrowIfNull(
            quote);

        if (string.IsNullOrWhiteSpace(
            Reason))
        {
            throw new InvalidOperationException(
                "Debes indicar el motivo de la liquidación anticipada.");
        }

        return new EarlySettlementExecuteRequest
        {
            DiscountType =
                DiscountType,

            DiscountValue =
                DiscountValue,

            ExpectedSettlementAmount =
                quote.SettlementAmount,

            Reason =
                Reason.Trim()
        };
    }

    public void ResetQuoteValues()
    {
        Reason =
            null;
    }

    public static EarlySettlementFormModel
        ForLoan(
            Guid loanId)
    {
        return new EarlySettlementFormModel
        {
            LoanId =
                loanId,

            DiscountType =
                EarlySettlementDiscountType.InstallmentWaiver,

            DiscountValue =
                1m
        };
    }
}