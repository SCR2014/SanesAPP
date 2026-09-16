namespace Sanes.Domain.Entities;

public class AppUserCollectionRoute
{
    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public Guid CollectionRouteId { get; set; }
    public CollectionRoute CollectionRoute { get; set; } = null!;

    public DateTime AssignedAt { get; set; }
}