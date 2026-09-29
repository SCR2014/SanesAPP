using System.ComponentModel.DataAnnotations;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Payments.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Web.Payments;

public sealed class PaymentFormModel
    : IValidatableObject
{
    public Guid LoanId { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "79228162514264337593543950335",
        ErrorMessage =
            "El monto debe ser mayor que cero.")]
    public decimal Amount { get; set; }

    public PaymentType PaymentType { get; set; }
        = PaymentType.Regular;

    [MaxLength(
        1000,
        ErrorMessage =
            "Las notas no pueden exceder 1000 caracteres.")]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (LoanId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Debe seleccionar un préstamo.",
                [nameof(LoanId)]);
        }

        if (!Enum.IsDefined(
            typeof(PaymentType),
            PaymentType))
        {
            yield return new ValidationResult(
                "El tipo de pago no es válido.",
                [nameof(PaymentType)]);
        }
    }

    public CreatePaymentRequest ToRequest()
    {
        return new CreatePaymentRequest
        {
            LoanId =
                LoanId,

            Amount =
                Amount,

            /*
             * Igual que Field Collections:
             * el pago normal se registra con el
             * instante real de la operación.
             */
            PaymentDate =
                DateTime.UtcNow,

            PaymentType =
                PaymentType,

            Notes =
                string.IsNullOrWhiteSpace(
                    Notes)
                    ? null
                    : Notes.Trim(),

            CollectedByAppUserId =
                null,

            CollectionRouteId =
                null
        };
    }

    public static PaymentFormModel ForLoan(
        Guid loanId,
        LoanFinancialSummaryResponse summary)
    {
        ArgumentNullException.ThrowIfNull(
            summary);

        var suggestedAmount =
            summary.IsOverdue &&
            summary.TotalOverdueAmountDue > 0
                ? summary.TotalOverdueAmountDue
                : summary.NextInstallmentAmountDue;

        if (suggestedAmount <= 0)
        {
            suggestedAmount =
                summary.TotalOutstanding;
        }

        return new PaymentFormModel
        {
            LoanId =
                loanId,

            Amount =
                suggestedAmount,

            PaymentType =
                PaymentType.Regular
        };
    }
}