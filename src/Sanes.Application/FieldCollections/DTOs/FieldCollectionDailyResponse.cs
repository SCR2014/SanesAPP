namespace Sanes.Application.FieldCollections.DTOs;

public class FieldCollectionDailyResponse
{
    public DateOnly Date { get; set; }

    public Guid AppUserId { get; set; }

    public string CollectorName { get; set; } = string.Empty;

    public int RoutesCount { get; set; }

    public int ClientsCount { get; set; }

    public int LoansCount { get; set; }

    public decimal TotalBalance { get; set; }

    public decimal TotalLateFeeBalance { get; set; }

    public decimal TotalOutstanding { get; set; }

    public decimal TotalOverdueAmount { get; set; }

    public decimal TotalOverdueAmountDue { get; set; }

    public decimal TotalAmountDue { get; set; }

    public decimal TotalCollectionAmountDue { get; set; }

    public List<FieldCollectionRouteResponse> Routes { get; set; }
        = new();
}