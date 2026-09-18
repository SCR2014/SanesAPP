using Sanes.Domain.Enums;

namespace Sanes.Application.FinancialReports.DTOs;

public class FinancialPortfolioReportItemResponse
{
    public Guid LoanId { get; set; }

    public Guid ClientId { get; set; }

    public string ClientName { get; set; } =
        string.Empty;

    public string ClientPhone { get; set; } =
        string.Empty;

    public string? ClientAddress { get; set; }

    public Guid InvestorId { get; set; }

    public string InvestorName { get; set; } =
        string.Empty;

    /*
     * Ruta ACTUAL del cliente.
     *
     * Este reporte es una fotografía actual
     * de la cartera, no una atribución histórica.
     */
    public Guid? CollectionRouteId { get; set; }

    public string? CollectionRouteName { get; set; }

    public decimal PrincipalAmount { get; set; }

    public decimal ContractualAmount { get; set; }

    public decimal ContractualBalanceOutstanding { get; set; }

    public decimal LateFeeBalanceOutstanding { get; set; }

    public decimal TotalOutstanding { get; set; }

    public decimal InstallmentAmount { get; set; }

    public decimal NextInstallmentAmountDue { get; set; }

    public DateTime NextPaymentDate { get; set; }

    public bool IsOverdue { get; set; }

    public int DaysOverdue { get; set; }

    public int OverdueInstallments { get; set; }

    public decimal OverdueContractualAmount { get; set; }

    public decimal TotalOverdueAmountDue { get; set; }

    public bool HasOutstandingLateFees { get; set; }

    public decimal PercentagePaid { get; set; }

    public PaymentFrequency PaymentFrequency { get; set; }
}