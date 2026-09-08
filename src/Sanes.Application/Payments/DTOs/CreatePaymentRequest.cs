using System.ComponentModel.DataAnnotations;
using Sanes.Domain.Enums;

namespace Sanes.Application.Payments.DTOs;

public class CreatePaymentRequest : IValidatableObject
{
    [Required]
    public Guid TenantId { get; set; }

    [Required]
    public Guid LoanId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    [Required]
    public PaymentType PaymentType { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (TenantId == Guid.Empty)
        {
            yield return new ValidationResult(
                "TenantId must be a valid identifier.",
                new[] { nameof(TenantId) });
        }

        if (LoanId == Guid.Empty)
        {
            yield return new ValidationResult(
                "LoanId must be a valid identifier.",
                new[] { nameof(LoanId) });
        }

        if (PaymentDate == default)
        {
            yield return new ValidationResult(
                "PaymentDate is required.",
                new[] { nameof(PaymentDate) });
        }

        if (!Enum.IsDefined(typeof(PaymentType), PaymentType))
        {
            yield return new ValidationResult(
                "PaymentType is invalid.",
                new[] { nameof(PaymentType) });
        }
    }
}