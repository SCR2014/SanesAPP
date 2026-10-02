namespace Sanes.Domain.Entities;

public class PaymentReversal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public Tenant Tenant { get; set; } = null!;

    public Guid PaymentId { get; set; }

    public Payment Payment { get; set; } = null!;

    public Guid ReversedByAppUserId { get; set; }

    public AppUser ReversedByAppUser { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}