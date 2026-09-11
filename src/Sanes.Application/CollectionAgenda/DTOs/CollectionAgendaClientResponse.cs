namespace Sanes.Application.CollectionAgenda.DTOs;

public class CollectionAgendaClientResponse
{
    public Guid ClientId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string? Address { get; set; }

    public int? CollectionRouteOrder { get; set; }
}