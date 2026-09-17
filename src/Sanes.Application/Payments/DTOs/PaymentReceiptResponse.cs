using Sanes.Domain.Enums;

namespace Sanes.Application.Payments.DTOs;

public class PaymentReceiptResponse
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public string ReceiptNumber { get; set; } =
        string.Empty;

    public int ReceiptYear { get; set; }

    public int SequenceNumber { get; set; }

    /*
     * Tenant snapshot.
     */
    public string TenantName { get; set; } =
        string.Empty;

    public string? TenantLegalName { get; set; }

    public string CurrencyCode { get; set; } =
        string.Empty;

    public string CurrencySymbol { get; set; } =
        string.Empty;

    /*
     * Client snapshot.
     */
    public Guid ClientId { get; set; }

    public string ClientName { get; set; } =
        string.Empty;

    /*
     * Payment / loan.
     */
    public Guid LoanId { get; set; }

    public DateTime PaymentDate { get; set; }

    public PaymentType PaymentType { get; set; }

    public decimal AmountReceived { get; set; }

    public decimal LateFeeAmountApplied { get; set; }

    public decimal LoanBalanceAmountApplied { get; set; }

    public decimal ContractualBalanceAfter { get; set; }

    public decimal LateFeeBalanceAfter { get; set; }

    public decimal TotalOutstandingAfter { get; set; }

    public Guid? CollectedByAppUserId { get; set; }

    public string? CollectedByName { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}