using Sanes.Domain.Enums;

namespace Sanes.Domain.Entities;

public class PaymentReceipt
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    /*
     * Visible receipt number.
     *
     * Example:
     * REC-2026-000001
     */
    public string ReceiptNumber { get; set; } =
        string.Empty;

    public int ReceiptYear { get; set; }

    public int SequenceNumber { get; set; }

    /*
     * Tenant snapshot at receipt creation time.
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
     * Loan and payment information.
     */
    public Guid LoanId { get; set; }

    public DateTime PaymentDate { get; set; }

    public PaymentType PaymentType { get; set; }

    /*
     * Actual cash received.
     *
     * Always corresponds to Payment.Amount.
     */
    public decimal AmountReceived { get; set; }

    /*
     * Distribution of the received payment.
     */
    public decimal LateFeeAmountApplied { get; set; }

    public decimal LoanBalanceAmountApplied { get; set; }

    /*
     * Balances immediately after the payment.
     */
    public decimal ContractualBalanceAfter { get; set; }

    public decimal LateFeeBalanceAfter { get; set; }

    public decimal TotalOutstandingAfter { get; set; }

    /*
     * Collector information when applicable.
     */
    public Guid? CollectedByAppUserId { get; set; }

    public AppUser? CollectedByAppUser { get; set; }

    /*
     * Historical snapshot of the collector name.
     */
    public string? CollectedByName { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;
}