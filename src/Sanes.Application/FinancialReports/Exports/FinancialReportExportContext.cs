namespace Sanes.Application.FinancialReports.Exports;

public class FinancialReportExportContext
{
    public string TenantName { get; set; } =
        string.Empty;

    public string CurrencyCode { get; set; } =
        string.Empty;

    public string CurrencySymbol { get; set; } =
        string.Empty;

    public DateTime GeneratedAtUtc { get; set; }

    /*
     * Texto descriptivo de los filtros aplicados.
     *
     * Ejemplos:
     * Investor: Juan Perez
     * Route: Santiago Centro
     * Overdue only: Yes
     */
    public List<string> Filters { get; set; } =
        new();
}