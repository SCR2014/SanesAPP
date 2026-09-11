using Sanes.Domain.Enums;

namespace Sanes.Application.CollectionRouteSchedules.DTOs;

public class UpdateCollectionRouteScheduleRequest
{
    public CollectionWeekDay DayOfWeek { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }
}