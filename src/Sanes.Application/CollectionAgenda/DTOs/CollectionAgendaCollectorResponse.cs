namespace Sanes.Application.CollectionAgenda.DTOs;

public class CollectionAgendaCollectorResponse
{
    public Guid AppUserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
}