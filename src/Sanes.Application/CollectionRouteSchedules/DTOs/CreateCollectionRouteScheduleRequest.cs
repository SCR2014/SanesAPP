using Sanes.Domain.Enums;

namespace Sanes.Application.CollectionRouteSchedules.DTOs;

public class CreateCollectionRouteScheduleRequest
{

    public Guid CollectionRouteId { get; set; }

    public CollectionWeekDay DayOfWeek { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }
}