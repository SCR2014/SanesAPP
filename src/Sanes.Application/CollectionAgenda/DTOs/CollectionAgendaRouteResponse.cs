namespace Sanes.Application.CollectionAgenda.DTOs;

public class CollectionAgendaRouteResponse
{
    public Guid CollectionRouteId { get; set; }

    public string CollectionRouteName { get; set; } = string.Empty;

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public List<CollectionAgendaCollectorResponse> Collectors { get; set; }
        = new();

    public List<CollectionAgendaClientResponse> Clients { get; set; }
        = new();
}