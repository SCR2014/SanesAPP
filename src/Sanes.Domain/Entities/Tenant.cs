namespace Sanes.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public string CurrencySymbol { get; set; } = "$";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}