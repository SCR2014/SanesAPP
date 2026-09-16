namespace Sanes.Application.FieldCollections.DTOs;

public class FieldCollectionLoanResponse
{
    public Guid LoanId { get; set; }

    // Saldo contractual del préstamo.
    public decimal Balance { get; set; }

    // Mora pendiente.
    public decimal LateFeeBalance { get; set; }

    // Balance + LateFeeBalance.
    public decimal TotalOutstanding { get; set; }

    public decimal InstallmentAmount { get; set; }

    // Parte contractual de la próxima cuota.
    public decimal NextInstallmentAmountDue { get; set; }

    /*
     * Monto operativo sugerido para el cobro:
     * próxima cuota contractual pendiente + mora pendiente.
     *
     * Se mantiene separado de OverdueAmount, ya que este último
     * puede representar varias cuotas contractuales vencidas.
     */
    public decimal CollectionAmountDue { get; set; }

    public DateTime NextPaymentDate { get; set; }

    // Deuda contractual vencida.
    public decimal OverdueAmount { get; set; }

    // Deuda contractual vencida + mora.
    public decimal TotalOverdueAmountDue { get; set; }

    public int DaysOverdue { get; set; }

    public int OverdueInstallments { get; set; }

    public bool IsOverdue { get; set; }

    public bool HasOutstandingLateFees { get; set; }
}