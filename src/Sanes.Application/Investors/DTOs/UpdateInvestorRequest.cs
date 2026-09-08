using System.ComponentModel.DataAnnotations;

namespace Sanes.Application.Investors.DTOs;

public class UpdateInvestorRequest : IValidatableObject
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? Identification { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "Name cannot be empty or contain only spaces.",
                new[] { nameof(Name) });
        }
    }
}