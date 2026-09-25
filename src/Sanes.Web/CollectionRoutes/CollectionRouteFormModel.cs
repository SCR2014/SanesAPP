using System.ComponentModel.DataAnnotations;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Web.CollectionRoutes;

public sealed class CollectionRouteFormModel
    : IValidatableObject
{
    [Required]
    public string Name { get; set; } =
        string.Empty;

    public string? Description { get; set; }

    public CollectionRouteOrderMode OrderMode { get; set; } =
        CollectionRouteOrderMode.Manual;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "El nombre de la ruta es requerido.",
                [nameof(Name)]);
        }

        if (!Enum.IsDefined(OrderMode))
        {
            yield return new ValidationResult(
                "El modo de ordenamiento no es válido.",
                [nameof(OrderMode)]);
        }
    }

    public CreateCollectionRouteRequest ToCreateRequest()
    {
        return new CreateCollectionRouteRequest
        {
            Name = Name,
            Description = Description,
            OrderMode = OrderMode
        };
    }

    public UpdateCollectionRouteRequest ToUpdateRequest()
    {
        return new UpdateCollectionRouteRequest
        {
            Name = Name,
            Description = Description,
            OrderMode = OrderMode
        };
    }

    public static CollectionRouteFormModel FromResponse(
        CollectionRouteResponse route)
    {
        return new CollectionRouteFormModel
        {
            Name = route.Name,
            Description = route.Description,
            OrderMode = route.OrderMode
        };
    }
}