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

    public decimal TotalLateFeeBalance { get; set; }

    public decimal TotalOutstanding { get; set; }

    public decimal TotalOverdueAmount { get; set; }

    public decimal TotalOverdueAmountDue { get; set; }

    /*
     * Se conserva el significado anterior:
     * suma de NextInstallmentAmountDue.
     */
    public decimal TotalAmountDue { get; set; }

    // Cuotas actuales pendientes + mora pendiente.
    public decimal TotalCollectionAmountDue { get; set; }

    public List<FieldCollectionClientResponse> Clients { get; set; }
        = new();

}