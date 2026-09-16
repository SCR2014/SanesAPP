namespace Sanes.Application.CollectionRoutes.DTOs;

using Sanes.Domain.Enums;

public class CollectionRouteResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public CollectionRouteOrderMode OrderMode { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}