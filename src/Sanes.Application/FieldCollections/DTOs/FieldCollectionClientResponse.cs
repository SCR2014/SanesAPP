namespace Sanes.Application.FieldCollections.DTOs;

public class FieldCollectionClientResponse
{
    public Guid ClientId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public int? CollectionRouteOrder { get; set; }

    public List<FieldCollectionLoanResponse> Loans { get; set; }
        = new();
}