namespace Sanes.Application.Tenants.DTOs;

public class TenantDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}