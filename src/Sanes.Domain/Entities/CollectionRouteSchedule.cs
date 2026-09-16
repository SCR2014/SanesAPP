using Sanes.Domain.Enums;

namespace Sanes.Domain.Entities;

public class CollectionRouteSchedule
{
    public Guid Id { get; set; }

    public Guid CollectionRouteId { get; set; }
    public CollectionRoute CollectionRoute { get; set; } = null!;

    public CollectionWeekDay DayOfWeek { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}