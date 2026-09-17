namespace Sanes.Application.Dashboard.Models;

public class FinancialDashboardRouteData
{
    public Guid CollectionRouteId { get; set; }

    public string CollectionRouteName { get; set; } =
        string.Empty;

    public bool IsActive { get; set; }
}