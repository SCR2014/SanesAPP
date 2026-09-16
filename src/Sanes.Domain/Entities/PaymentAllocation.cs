using Sanes.Domain.Enums;

namespace Sanes.Domain.Entities;

public class PaymentAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    public PaymentAllocationType AllocationType { get; set; }

    public Guid? LateFeeChargeId { get; set; }
    public LateFeeCharge? LateFeeCharge { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}