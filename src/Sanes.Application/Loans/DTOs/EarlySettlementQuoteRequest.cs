using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.DTOs;

public class EarlySettlementQuoteRequest
{
    public EarlySettlementDiscountType DiscountType { get; set; }

    /*
     * InstallmentWaiver:
     * 1, 2 o 3 cuotas.
     *
     * PercentageDiscount:
     * porcentaje, por ejemplo 15 = 15%.
     */
    public decimal DiscountValue { get; set; }
}