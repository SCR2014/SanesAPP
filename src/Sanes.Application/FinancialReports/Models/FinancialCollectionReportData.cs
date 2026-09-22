using Sanes.Domain.Enums;

namespace Sanes.Application.FinancialReports.Models;

public class FinancialCollectionReportData
{
    public Guid PaymentId { get; set; }

    public string? ReceiptNumber { get; set; }

    public DateTime PaymentDate { get; set; }

    public PaymentType PaymentType { get; set; }

    public Guid LoanId { get; set; }

    public Guid ClientId { get; set; }

    public string ClientName { get; set; } =
        string.Empty;

    public Guid InvestorId { get; set; }

    public string InvestorName { get; set; } =
        string.Empty;

    public Guid? CollectedByAppUserId { get; set; }

    public string? CollectorName { get; set; }

    /*
     * Ruta almacenada históricamente en Payment.
     *
     * NO corresponde necesariamente a la ruta
     * actual del cliente.
     */
    public Guid? CollectionRouteId { get; set; }

    public string? CollectionRouteName { get; set; }

    /*
     * Payment.Amount:
     * efectivo realmente recibido.
     */
    public decimal CashCollected { get; set; }

    /*
     * Allocations LoanBalance.
     */
    public decimal ContractualCashCollected { get; set; }

    /*
     * Allocations LateFee.
     */
    public decimal LateFeesCollected { get; set; }
}