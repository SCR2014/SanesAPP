namespace Sanes.Domain.Entities;
using Sanes.Domain.Enums;

public class CollectionRoute
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public CollectionRouteOrderMode OrderMode { get; set; }
        = CollectionRouteOrderMode.Manual;

    public ICollection<AppUserCollectionRoute> AssignedUsers { get; set; }
        = new List<AppUserCollectionRoute>();

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}