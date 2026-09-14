using System.ComponentModel.DataAnnotations;

namespace Sanes.Application.Provisioning.DTOs;

public class ProvisionTenantRequest
{
    [Required]
    public ProvisionTenantData Tenant { get; set; } = new();

    [Required]
    public ProvisionAdministratorData Administrator { get; set; } = new();
}

public class ProvisionTenantData : IValidatableObject
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
    public string CurrencyCode { get; set; } = "DOP";

    [Required]
    [MaxLength(10)]
    public string CurrencySymbol { get; set; } = "RD$";

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "Tenant name is required.",
                new[] { nameof(Name) });
        }

        if (string.IsNullOrWhiteSpace(CurrencyCode))
        {
            yield return new ValidationResult(
                "CurrencyCode is required.",
                new[] { nameof(CurrencyCode) });
        }

        if (string.IsNullOrWhiteSpace(CurrencySymbol))
        {
            yield return new ValidationResult(
                "CurrencySymbol is required.",
                new[] { nameof(CurrencySymbol) });
        }
    }
}

public class ProvisionAdministratorData
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }
}