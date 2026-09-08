using System.ComponentModel.DataAnnotations;

namespace Sanes.Application.Clients.DTOs;

public class CreateClientRequest : IValidatableObject
{
    [Required]
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? LastName { get; set; }

    [Required]
    [MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? SecondaryPhone { get; set; }

    [MaxLength(50)]
    public string? IdentificationType { get; set; }

    [MaxLength(100)]
    public string? Identification { get; set; }

    [MaxLength(150)]
    public string? SocialNumber { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TenantId == Guid.Empty)
        {
            yield return new ValidationResult(
                "TenantId must be a valid identifier.",
                new[] { nameof(TenantId) });
        }

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            yield return new ValidationResult(
                "FirstName cannot be empty or contain only spaces.",
                new[] { nameof(FirstName) });
        }

        if (string.IsNullOrWhiteSpace(Phone))
        {
            yield return new ValidationResult(
                "Phone cannot be empty or contain only spaces.",
                new[] { nameof(Phone) });
        }

        if (Latitude is < -90 or > 90)
        {
            yield return new ValidationResult(
                "Latitude must be between -90 and 90.",
                new[] { nameof(Latitude) });
        }

        if (Longitude is < -180 or > 180)
        {
            yield return new ValidationResult(
                "Longitude must be between -180 and 180.",
                new[] { nameof(Longitude) });
        }
    }
}