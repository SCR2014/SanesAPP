using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Sanes.Application.CollectionRouteSchedules.DTOs;
using Sanes.Domain.Enums;
using System.Globalization;

namespace Sanes.Web.CollectionRouteSchedules;

public sealed class CollectionRouteScheduleFormModel
    : IValidatableObject
{
    public CollectionWeekDay DayOfWeek { get; set; } =
        CollectionWeekDay.Monday;

    public string? StartTime { get; set; }

    public string? EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!Enum.IsDefined(DayOfWeek))
        {
            yield return new ValidationResult(
                "Debes seleccionar un día válido.",
                [nameof(DayOfWeek)]);
        }

        var hasStartTime =
            !string.IsNullOrWhiteSpace(StartTime);

        var hasEndTime =
            !string.IsNullOrWhiteSpace(EndTime);

        if (hasStartTime != hasEndTime)
        {
            yield return new ValidationResult(
                "La hora de inicio y la hora de fin deben proporcionarse juntas.",
                [nameof(StartTime), nameof(EndTime)]);
        }

        if (!TryParseTime(
                StartTime,
                out var startTime))
        {
            yield return new ValidationResult(
                "La hora de inicio no es válida.",
                [nameof(StartTime)]);
        }

        if (!TryParseTime(
                EndTime,
                out var endTime))
        {
            yield return new ValidationResult(
                "La hora de fin no es válida.",
                [nameof(EndTime)]);
        }

        if (startTime.HasValue &&
            endTime.HasValue &&
            endTime.Value <= startTime.Value)
        {
            yield return new ValidationResult(
                "La hora de fin debe ser posterior a la hora de inicio.",
                [nameof(StartTime), nameof(EndTime)]);
        }
    }

    public CreateCollectionRouteScheduleRequest ToCreateRequest(
        Guid collectionRouteId)
    {
        TryParseTime(
            StartTime,
            out var startTime);

        TryParseTime(
            EndTime,
            out var endTime);

        return new CreateCollectionRouteScheduleRequest
        {
            CollectionRouteId = collectionRouteId,
            DayOfWeek = DayOfWeek,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    public UpdateCollectionRouteScheduleRequest ToUpdateRequest()
    {
        TryParseTime(
            StartTime,
            out var startTime);

        TryParseTime(
            EndTime,
            out var endTime);

        return new UpdateCollectionRouteScheduleRequest
        {
            DayOfWeek = DayOfWeek,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    public static CollectionRouteScheduleFormModel FromResponse(
        CollectionRouteScheduleResponse schedule)
    {
        return new CollectionRouteScheduleFormModel
        {
            DayOfWeek =
                schedule.DayOfWeek,

            StartTime =
                FormatTime(
                    schedule.StartTime),

            EndTime =
                FormatTime(
                    schedule.EndTime)
        };
    }

    private static readonly string[] SupportedTimeFormats =
    [
        "HH:mm",
        "H:mm",
        "HH:mm:ss",
        "H:mm:ss",
        "hh:mm tt",
        "h:mm tt",
        "hh:mm:ss tt",
        "h:mm:ss tt"
    ];

    private static readonly CultureInfo[] SupportedTimeCultures =
    [
        CultureInfo.InvariantCulture,
        CultureInfo.CurrentCulture,
        new CultureInfo("en-US"),
        new CultureInfo("es-DO")
    ];

    private static bool TryParseTime(
        string? value,
        out TimeOnly? parsedTime)
    {
        parsedTime = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized =
            value.Trim();

        foreach (var culture in SupportedTimeCultures)
        {
            if (TimeOnly.TryParseExact(
                normalized,
                SupportedTimeFormats,
                culture,
                DateTimeStyles.AllowWhiteSpaces,
                out var exactTime))
            {
                parsedTime = exactTime;
                return true;
            }

            if (TimeOnly.TryParse(
                normalized,
                culture,
                DateTimeStyles.AllowWhiteSpaces,
                out var flexibleTime))
            {
                parsedTime = flexibleTime;
                return true;
            }
        }

        return false;
    }

    private static TimeOnly? ParseOptionalTime(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(
            value))
        {
            return null;
        }

        return TryParseTime(
            value,
            out var result)
                ? result
                : null;
    }

    private static string? FormatTime(
        TimeOnly? value)
    {
        return value?.ToString(
            "HH:mm",
            CultureInfo.InvariantCulture);
    }
}