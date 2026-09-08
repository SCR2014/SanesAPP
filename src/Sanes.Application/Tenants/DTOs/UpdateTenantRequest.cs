using System.ComponentModel.DataAnnotations;

namespace Sanes.Application.Tenants.DTOs;

public class UpdateTenantRequest : IValidatableObject
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? LegalName { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string CurrencyCode { get; set; } = "USD";

    [Required]
    [MaxLength(10)]
    public string CurrencySymbol { get; set; } = "$";

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "Name cannot be empty or contain only spaces.",
                new[] { nameof(Name) });
        }

        if (string.IsNullOrWhiteSpace(CurrencyCode))
        {
            yield return new ValidationResult(
                "CurrencyCode cannot be empty or contain only spaces.",
                new[] { nameof(CurrencyCode) });
        }

        if (string.IsNullOrWhiteSpace(CurrencySymbol))
        {
            yield return new ValidationResult(
                "CurrencySymbol cannot be empty or contain only spaces.",
                new[] { nameof(CurrencySymbol) });
        }
    }
}