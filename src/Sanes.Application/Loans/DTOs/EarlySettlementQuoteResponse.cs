using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.DTOs;

public class EarlySettlementQuoteResponse
{
    public Guid LoanId { get; set; }

    public bool IsEligible { get; set; }

    public int MinimumRequiredInstallments { get; set; }

    public int CompletedInstallments { get; set; }

    public decimal InstallmentAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal ContractualAdjustments { get; set; }

    public decimal ContractualBalance { get; set; }

    public decimal LateFeeBalance { get; set; }

    public decimal TotalOutstanding { get; set; }

    public EarlySettlementDiscountType DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal SettlementAmount { get; set; }

    public DateTime QuotedAt { get; set; }
}