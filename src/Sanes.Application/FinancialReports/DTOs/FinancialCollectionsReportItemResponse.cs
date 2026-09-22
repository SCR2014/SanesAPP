using Sanes.Domain.Enums;

namespace Sanes.Application.FinancialReports.DTOs;

public class FinancialCollectionsReportItemResponse
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

    /*
     * Cobrador registrado en el momento del pago.
     *
     * Null para pagos que no fueron realizados
     * mediante cobranza en campo.
     */
    public Guid? CollectedByAppUserId { get; set; }

    public string? CollectorName { get; set; }

    /*
     * Ruta histórica almacenada en Payment.
     *
     * NO corresponde necesariamente a la ruta
     * actual del cliente.
     */
    public Guid? CollectionRouteId { get; set; }

    public string? CollectionRouteName { get; set; }

    /*
     * Efectivo real recibido.
     */
    public decimal CashCollected { get; set; }

    /*
     * Parte del efectivo aplicada al saldo
     * contractual.
     */
    public decimal ContractualCashCollected { get; set; }

    /*
     * Parte del efectivo aplicada a mora.
     */
    public decimal LateFeesCollected { get; set; }
}