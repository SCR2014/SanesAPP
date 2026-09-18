namespace Sanes.Application.FinancialReports.Models;

public class FinancialReportRouteData
{
    public Guid CollectionRouteId { get; set; }

    public string CollectionRouteName { get; set; } =
        string.Empty;

    public bool IsActive { get; set; }
}