using System.ComponentModel.DataAnnotations;
using Sanes.Domain.Enums;

namespace Sanes.Application.Tenants.DTOs;

public class CreateTenantRequest : IValidatableObject
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

    public bool DefaultLateFeeEnabled { get; set; } = false;

    public LateFeeCalculationType DefaultLateFeeCalculationType { get; set; }
        = LateFeeCalculationType.FixedAmountPerInstallment;

    public decimal DefaultLateFeeAmount { get; set; } = 0m;

    public int DefaultLateFeeGraceDays { get; set; } = 0;

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

        if (!Enum.IsDefined(
                typeof(LateFeeCalculationType),
                DefaultLateFeeCalculationType))
        {
            yield return new ValidationResult(
                "DefaultLateFeeCalculationType is invalid.",
                new[] { nameof(DefaultLateFeeCalculationType) });
        }

        if (DefaultLateFeeAmount < 0)
        {
            yield return new ValidationResult(
                "DefaultLateFeeAmount cannot be negative.",
                new[] { nameof(DefaultLateFeeAmount) });
        }

        if (DefaultLateFeeGraceDays < 0)
        {
            yield return new ValidationResult(
                "DefaultLateFeeGraceDays cannot be negative.",
                new[] { nameof(DefaultLateFeeGraceDays) });
        }

        if (
            DefaultLateFeeEnabled &&
            DefaultLateFeeCalculationType ==
                LateFeeCalculationType.FixedAmountPerInstallment &&
            DefaultLateFeeAmount <= 0)
        {
            yield return new ValidationResult(
                "DefaultLateFeeAmount must be greater than zero when late fees are enabled.",
                new[] { nameof(DefaultLateFeeAmount) });
        }
    }
}