using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.DTOs;

public class EarlySettlementExecuteRequest
{
    public EarlySettlementDiscountType DiscountType { get; set; }

    /*
     * InstallmentWaiver:
     * 1, 2 o 3.
     *
     * PercentageDiscount:
     * porcentaje, por ejemplo 15 = 15%.
     */
    public decimal DiscountValue { get; set; }

    /*
     * Monto mostrado previamente al administrador
     * por el endpoint de cotización.
     *
     * El backend recalculará todo y rechazará la
     * operación si el monto cambió.
     */
    public decimal ExpectedSettlementAmount { get; set; }

    /*
     * Motivo/auditoría del beneficio concedido.
     */
    public string Reason { get; set; } = string.Empty;
}