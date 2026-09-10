namespace Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Domain.Enums;
public class UpdateCollectionRouteRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public CollectionRouteOrderMode? OrderMode { get; set; }
}