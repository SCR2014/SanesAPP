using System.ComponentModel.DataAnnotations;
using Sanes.Application.FieldCollections.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Web.FieldCollections;

public sealed class FieldCollectionPaymentFormModel
{
    [Range(
        typeof(decimal),
        "0.01",
        "79228162514264337593543950335",
        ErrorMessage =
            "El monto debe ser mayor que cero.")]
    public decimal Amount { get; set; }

    public PaymentType PaymentType { get; set; } =
        PaymentType.Regular;

    [MaxLength(
        1000,
        ErrorMessage =
            "Las notas no pueden exceder 1000 caracteres.")]
    public string? Notes { get; set; }

    public CreateFieldCollectionPaymentRequest ToRequest(
        Guid collectionRouteId,
        Guid loanId)
    {
        return new CreateFieldCollectionPaymentRequest
        {
            CollectionRouteId =
                collectionRouteId,

            LoanId =
                loanId,

            Amount =
                Amount,

            PaymentType =
                PaymentType,

            Notes =
                string.IsNullOrWhiteSpace(
                    Notes)
                    ? null
                    : Notes.Trim()
        };
    }

    public static FieldCollectionPaymentFormModel ForLoan(
        FieldCollectionLoanResponse loan)
    {
        return new FieldCollectionPaymentFormModel
        {
            Amount =
                loan.CollectionAmountDue > 0
                    ? loan.CollectionAmountDue
                    : loan.TotalOutstanding,

            PaymentType =
                PaymentType.Regular
        };
    }
}