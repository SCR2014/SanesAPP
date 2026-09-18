using Sanes.Domain.Enums;

namespace Sanes.Application.FinancialReports.DTOs;

public class FinancialDelinquencyReportItemResponse
{
    public Guid LoanId { get; set; }

    public Guid ClientId { get; set; }

    public string ClientName { get; set; } =
        string.Empty;

    public string ClientPhone { get; set; } =
        string.Empty;

    public Guid InvestorId { get; set; }

    public string InvestorName { get; set; } =
        string.Empty;

    public Guid? CollectionRouteId { get; set; }

    public string? CollectionRouteName { get; set; }

    public decimal ContractualBalanceOutstanding { get; set; }

    public decimal LateFeeBalanceOutstanding { get; set; }

    public decimal TotalOutstanding { get; set; }

    public decimal InstallmentAmount { get; set; }

    public decimal NextInstallmentAmountDue { get; set; }

    public DateTime NextPaymentDate { get; set; }

    public int DaysOverdue { get; set; }

    public int OverdueInstallments { get; set; }

    public decimal OverdueContractualAmount { get; set; }

    public decimal TotalOverdueAmountDue { get; set; }

    public bool HasOutstandingLateFees { get; set; }

    /*
     * 1-7
     * 8-14
     * 15-30
     * 31-60
     * 61+
     * LateFeeOnly
     */
    public string AgingBucket { get; set; } =
        string.Empty;

    public PaymentFrequency PaymentFrequency { get; set; }
}