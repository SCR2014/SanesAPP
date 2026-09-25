using System.ComponentModel.DataAnnotations;
using Sanes.Application.CollectionRoutes.DTOs;

namespace Sanes.Web.CollectionRoutes;

public sealed class OptimizeCollectionRouteFormModel
    : IValidatableObject
{
    public decimal? StartLatitude { get; set; }

    public decimal? StartLongitude { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var hasLatitude =
            StartLatitude.HasValue;

        var hasLongitude =
            StartLongitude.HasValue;

        if (hasLatitude != hasLongitude)
        {
            yield return new ValidationResult(
                "La latitud y longitud de inicio deben proporcionarse juntas.",
                [
                    nameof(StartLatitude),
                    nameof(StartLongitude)
                ]);

            yield break;
        }

        if (StartLatitude is < -90m or > 90m)
        {
            yield return new ValidationResult(
                "La latitud debe estar entre -90 y 90.",
                [nameof(StartLatitude)]);
        }

        if (StartLongitude is < -180m or > 180m)
        {
            yield return new ValidationResult(
                "La longitud debe estar entre -180 y 180.",
                [nameof(StartLongitude)]);
        }
    }

    public OptimizeCollectionRouteRequest ToRequest()
    {
        return new OptimizeCollectionRouteRequest
        {
            StartLatitude = StartLatitude,
            StartLongitude = StartLongitude
        };
    }
}