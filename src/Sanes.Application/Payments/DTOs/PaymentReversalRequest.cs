using System.ComponentModel.DataAnnotations;

namespace Sanes.Application.Payments.DTOs;

public class PaymentReversalRequest : IValidatableObject
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Reason))
        {
            yield return new ValidationResult(
                "Reversal reason is required.",
                new[] { nameof(Reason) });
        }
    }
}