using Sanes.Domain.Enums;

namespace Sanes.Domain.Entities;

public class AppUser
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public AppUserRole Role { get; set; }

    public ICollection<AppUserCollectionRoute> CollectionRoutes { get; set; }
        = new List<AppUserCollectionRoute>();

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}