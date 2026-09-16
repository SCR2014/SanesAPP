namespace Sanes.Application.AppUsers.DTOs;

public class AppUserCollectionRouteResponse
{
    public Guid CollectionRouteId { get; set; }

    public string CollectionRouteName { get; set; } = string.Empty;

    public DateTime AssignedAt { get; set; }
}