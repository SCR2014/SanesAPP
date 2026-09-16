using Sanes.Domain.Enums;

namespace Sanes.Domain.Entities;

public class LateFeeCharge
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid LoanId { get; set; }
    public Loan Loan { get; set; } = null!;

    public int InstallmentNumber { get; set; }

    public DateTime InstallmentDueDate { get; set; }

    public DateTime EffectiveDate { get; set; }

    public LateFeeCalculationType CalculationType { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LateFeeAdjustment> Adjustments { get; set; }
        = new List<LateFeeAdjustment>();

    public ICollection<PaymentAllocation> PaymentAllocations { get; set; }
    = new List<PaymentAllocation>();
}