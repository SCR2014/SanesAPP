namespace Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Domain.Enums;

public class CreateCollectionRouteRequest
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public CollectionRouteOrderMode OrderMode { get; set; }
    = CollectionRouteOrderMode.Manual;
}