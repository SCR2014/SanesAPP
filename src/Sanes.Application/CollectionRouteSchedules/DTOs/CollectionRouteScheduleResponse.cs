using Sanes.Domain.Enums;

namespace Sanes.Application.CollectionRouteSchedules.DTOs;

public class CollectionRouteScheduleResponse
{
    public Guid Id { get; set; }

    public Guid CollectionRouteId { get; set; }

    public CollectionWeekDay DayOfWeek { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}