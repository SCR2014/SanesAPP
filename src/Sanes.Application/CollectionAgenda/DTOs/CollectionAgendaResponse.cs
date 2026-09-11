using Sanes.Domain.Enums;

namespace Sanes.Application.CollectionAgenda.DTOs;

public class CollectionAgendaResponse
{
    public DateOnly Date { get; set; }

    public CollectionWeekDay DayOfWeek { get; set; }

    public int RoutesCount { get; set; }

    public int CollectorsCount { get; set; }

    public int ClientsCount { get; set; }

    public List<CollectionAgendaRouteResponse> Routes { get; set; }
        = new();
}