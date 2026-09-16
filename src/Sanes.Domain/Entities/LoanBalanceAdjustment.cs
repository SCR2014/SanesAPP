using Sanes.Domain.Enums;

namespace Sanes.Domain.Entities;

public class LoanBalanceAdjustment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid LoanId { get; set; }
    public Loan Loan { get; set; } = null!;

    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public LoanBalanceAdjustmentType AdjustmentType { get; set; }

    /*
     * Siempre positivo.
     *
     * El tipo de ajuste determina cómo afecta el saldo.
     * EarlySettlementDiscount reduce el saldo contractual.
     */
    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}