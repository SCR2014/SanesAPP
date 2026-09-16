namespace Sanes.Application.Clients.DTOs;

public class ClientResponse
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }

    public string Phone { get; set; } = string.Empty;
    public string? SecondaryPhone { get; set; }

    public string? IdentificationType { get; set; }
    public string? Identification { get; set; }

    public string? SocialNumber { get; set; }

    public string? Address { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public Guid? CollectionRouteId { get; set; }
    public int? CollectionRouteOrder { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}