using Sanes.Domain.Enums;

namespace Sanes.Domain.Entities;

public class LateFeeAdjustment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid LateFeeChargeId { get; set; }
    public LateFeeCharge LateFeeCharge { get; set; } = null!;

    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public LateFeeAdjustmentType AdjustmentType { get; set; }

    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}