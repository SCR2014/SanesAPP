using System.ComponentModel.DataAnnotations;
using Sanes.Domain.Enums;

namespace Sanes.Application.Payments.DTOs;

public class CreatePaymentRequest : IValidatableObject
{

    [Required]
    public Guid LoanId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    [Required]
    public PaymentType PaymentType { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public Guid? CollectedByAppUserId { get; set; }

    public Guid? CollectionRouteId { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {

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
        if (CollectedByAppUserId.HasValue &&
            CollectedByAppUserId.Value == Guid.Empty)
        {
            yield return new ValidationResult(
                "CollectedByAppUserId must be a valid identifier.",
                new[] { nameof(CollectedByAppUserId) });
        }

        if (CollectionRouteId.HasValue &&
            CollectionRouteId.Value == Guid.Empty)
        {
            yield return new ValidationResult(
                "CollectionRouteId must be a valid identifier.",
                new[] { nameof(CollectionRouteId) });
        }

        if (CollectedByAppUserId.HasValue !=
            CollectionRouteId.HasValue)
        {
            yield return new ValidationResult(
                "CollectedByAppUserId and CollectionRouteId must be provided together.",
                new[]
                {
                    nameof(CollectedByAppUserId),
                    nameof(CollectionRouteId)
                });
        }
    }
}