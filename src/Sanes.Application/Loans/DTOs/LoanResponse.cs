using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.DTOs;

public class LoanResponse
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid InvestorId { get; set; }

    public Guid ClientId { get; set; }

    public decimal PrincipalAmount { get; set; }

    public decimal InstallmentAmount { get; set; }

    public int TotalInstallments { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal InterestAmount { get; set; }

    public PaymentFrequency PaymentFrequency { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime NextPaymentDate { get; set; }

    public LoanStatus Status { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}