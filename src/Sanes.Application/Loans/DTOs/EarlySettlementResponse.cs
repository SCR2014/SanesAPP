using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.DTOs;

public class EarlySettlementResponse
{
    public Guid Id { get; set; }

    public Guid LoanId { get; set; }

    /*
     * Puede ser null cuando SettlementAmount = 0.
     */
    public Guid? PaymentId { get; set; }

    public Guid LoanBalanceAdjustmentId { get; set; }

    public int CompletedInstallments { get; set; }

    public decimal ContractualBalanceBefore { get; set; }

    public decimal LateFeeBalanceBefore { get; set; }

    public decimal TotalOutstandingBefore { get; set; }

    public EarlySettlementDiscountType DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public decimal DiscountAmount { get; set; }

    /*
     * Efectivo realmente recibido.
     */
    public decimal SettlementAmount { get; set; }

    /*
     * Distribución del efectivo recibido.
     */
    public decimal AppliedToLateFees { get; set; }

    public decimal AppliedToLoan { get; set; }

    /*
     * Una liquidación ejecutada correctamente
     * debe cerrar completamente estos saldos.
     */
    public decimal ContractualBalanceAfter { get; set; }

    public decimal LateFeeBalanceAfter { get; set; }

    public decimal TotalOutstandingAfter { get; set; }

    public DateTime CreatedAt { get; set; }
}