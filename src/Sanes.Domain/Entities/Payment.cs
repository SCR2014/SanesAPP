using Sanes.Domain.Enums;

namespace Sanes.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid LoanId { get; set; }
    public Loan Loan { get; set; } = null!;

    public Guid? CollectedByAppUserId { get; set; }
    public AppUser? CollectedByAppUser { get; set; }

    public Guid? CollectionRouteId { get; set; }
    public CollectionRoute? CollectionRoute { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public PaymentType PaymentType { get; set; }

    public ICollection<PaymentAllocation> Allocations { get; set; }
        = new List<PaymentAllocation>();

    public EarlySettlement? EarlySettlement { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}