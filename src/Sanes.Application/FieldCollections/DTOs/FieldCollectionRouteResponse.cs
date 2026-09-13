namespace Sanes.Application.FieldCollections.DTOs;

public class FieldCollectionRouteResponse
{
    public Guid CollectionRouteId { get; set; }

    public string CollectionRouteName { get; set; } = string.Empty;

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public int ClientsCount { get; set; }

    public int LoansCount { get; set; }

    public decimal TotalBalance { get; set; }

    public decimal TotalOverdueAmount { get; set; }

    public decimal TotalAmountDue { get; set; }

    public List<FieldCollectionClientResponse> Clients { get; set; }
        = new();

}