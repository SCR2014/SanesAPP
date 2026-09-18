namespace Sanes.Application.FinancialReports.Models;

public class FinancialReportClientRouteData
{
    public Guid ClientId { get; set; }

    public Guid? CollectionRouteId { get; set; }

    public string? CollectionRouteName { get; set; }
}