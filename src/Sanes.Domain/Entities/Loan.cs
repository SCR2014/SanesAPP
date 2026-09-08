using Sanes.Domain.Enums;

namespace Sanes.Domain.Entities;

public class Loan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid InvestorId { get; set; }
    public Investor Investor { get; set; } = null!;

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public decimal PrincipalAmount { get; set; }

    public decimal InstallmentAmount { get; set; }

    public int TotalInstallments { get; set; }

    public PaymentFrequency PaymentFrequency { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime NextPaymentDate { get; set; }

    public LoanStatus Status { get; set; } = LoanStatus.Active;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}