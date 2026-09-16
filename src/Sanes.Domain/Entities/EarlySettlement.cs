using Sanes.Domain.Enums;

namespace Sanes.Domain.Entities;

public class EarlySettlement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid LoanId { get; set; }
    public Loan Loan { get; set; } = null!;

    /*
     * Administrador que aprobó/ejecutó
     * la liquidación anticipada.
     */
    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    /*
     * Pago real recibido del cliente.
     */
    /*
    *
    * Puede ser null cuando el descuento concedido
    * extingue completamente el saldo contractual
    * y no existe mora pendiente.
    */
    public Guid? PaymentId { get; set; }

    public Payment? Payment { get; set; }
    /*
     * Ajuste contractual correspondiente
     * al descuento concedido.
     */
    public Guid LoanBalanceAdjustmentId { get; set; }

    public LoanBalanceAdjustment LoanBalanceAdjustment { get; set; }
        = null!;

    public EarlySettlementDiscountType DiscountType { get; set; }

    /*
     * InstallmentWaiver:
     *   1, 2 o 3.
     *
     * PercentageDiscount:
     *   porcentaje aplicado.
     */
    public decimal DiscountValue { get; set; }

    /*
     * Snapshot de elegibilidad.
     */
    public int CompletedInstallments { get; set; }

    /*
     * Snapshot financiero previo a la liquidación.
     */
    public decimal ContractualBalanceBefore { get; set; }

    public decimal LateFeeBalanceBefore { get; set; }

    public decimal TotalOutstandingBefore { get; set; }

    /*
     * Beneficio contractual concedido.
     */
    public decimal DiscountAmount { get; set; }

    /*
     * Efectivo que realmente debe entregar
     * el cliente para cerrar el préstamo.
     *
     * ContractualBalanceBefore
     * - DiscountAmount
     * + LateFeeBalanceBefore
     */
    public decimal SettlementAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}