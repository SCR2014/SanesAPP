namespace Sanes.Application.Dashboard.DTOs;

public class FinancialDashboardRouteBreakdownResponse
{
    /*
     * Null solamente para el bucket "Sin ruta".
     */
    public Guid? CollectionRouteId { get; set; }

    public string CollectionRouteName { get; set; } =
        string.Empty;

    /*
     * Para "Sin ruta" será false.
     */
    public bool IsActive { get; set; }

    public bool IsUnassigned { get; set; }

    // Current portfolio.
    public int ActiveLoansCount { get; set; }

    public int ActiveClientsCount { get; set; }

    public decimal ContractualBalanceOutstanding { get; set; }

    public decimal LateFeeBalanceOutstanding { get; set; }

    public decimal TotalOutstanding { get; set; }

    /*
     * Parte contractual de las próximas cuotas
     * actualmente pendientes.
     */
    public decimal NextInstallmentAmountDue { get; set; }

    /*
     * NextInstallmentAmountDue +
     * LateFeeBalanceOutstanding.
     */
    public decimal CollectionAmountDue { get; set; }

    // Delinquency.
    public int OverdueLoansCount { get; set; }

    public decimal OverdueContractualAmount { get; set; }

    /*
     * OverdueContractualAmount +
     * mora pendiente correspondiente a la cartera.
     */
    public decimal TotalOverdueAmountDue { get; set; }

    public decimal DelinquencyRate { get; set; }
}