namespace Sanes.Application.CollectionRoutes.DTOs;

public class ReorderCollectionRouteRequest
{
    public List<Guid> ClientIds { get; set; } = new();
}