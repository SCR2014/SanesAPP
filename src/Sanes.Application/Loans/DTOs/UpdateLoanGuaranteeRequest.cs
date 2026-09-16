using System.ComponentModel.DataAnnotations;
using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.DTOs;

public class UpdateLoanGuaranteeRequest
    : IValidatableObject
{
    [Required]
    public LoanGuaranteeType Type { get; set; }

    [Required]
    [MaxLength(150)]
    public string Reference { get; set; }
        = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!Enum.IsDefined(
                typeof(LoanGuaranteeType),
                Type))
        {
            yield return new ValidationResult(
                "Guarantee type is invalid.",
                new[] { nameof(Type) });
        }

        if (string.IsNullOrWhiteSpace(
                Reference))
        {
            yield return new ValidationResult(
                "Guarantee reference is required.",
                new[] { nameof(Reference) });
        }
    }
}