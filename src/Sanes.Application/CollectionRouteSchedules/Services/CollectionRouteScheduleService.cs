using Sanes.Application.CollectionRoutes.Repositories;
using Sanes.Application.CollectionRouteSchedules.DTOs;
using Sanes.Application.CollectionRouteSchedules.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.CollectionRouteSchedules.Services;

public class CollectionRouteScheduleService
    : ICollectionRouteScheduleService
{
    private readonly ICollectionRouteScheduleRepository
        _scheduleRepository;

    private readonly ICollectionRouteRepository
        _collectionRouteRepository;

    public CollectionRouteScheduleService(
        ICollectionRouteScheduleRepository scheduleRepository,
        ICollectionRouteRepository collectionRouteRepository)
    {
        _scheduleRepository = scheduleRepository;
        _collectionRouteRepository = collectionRouteRepository;
    }

    public async Task<CollectionRouteScheduleResponse> CreateAsync(
        CreateCollectionRouteScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifiers(
            request.TenantId,
            request.CollectionRouteId);

        ValidateDayOfWeek(request.DayOfWeek);

        ValidateTimeRange(
            request.StartTime,
            request.EndTime);

        var collectionRoute =
            await _collectionRouteRepository.GetByIdAsync(
                request.CollectionRouteId,
                request.TenantId,
                cancellationToken);

        if (collectionRoute is null)
        {
            throw new ArgumentException(
                "Collection route does not exist, is inactive, or does not belong to this tenant.");
        }

        var dayExists =
            await _scheduleRepository.DayExistsAsync(
                request.CollectionRouteId,
                request.DayOfWeek,
                cancellationToken: cancellationToken);

        if (dayExists)
        {
            throw new ArgumentException(
                "This collection day is already configured for the route.");
        }

        var now = DateTime.UtcNow;

        var schedule =
            new CollectionRouteSchedule
            {
                Id = Guid.NewGuid(),
                CollectionRouteId =
                    request.CollectionRouteId,
                DayOfWeek =
                    request.DayOfWeek,
                StartTime =
                    request.StartTime,
                EndTime =
                    request.EndTime,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

        await _scheduleRepository.AddAsync(
            schedule,
            cancellationToken);

        await _scheduleRepository.SaveChangesAsync(
            cancellationToken);

        return Map(schedule);
    }

    public async Task<List<CollectionRouteScheduleResponse>>
        GetAllAsync(
            Guid tenantId,
            Guid collectionRouteId,
            CancellationToken cancellationToken = default)
    {
        ValidateIdentifiers(
            tenantId,
            collectionRouteId);

        var collectionRoute =
            await _collectionRouteRepository.GetByIdAsync(
                collectionRouteId,
                tenantId,
                cancellationToken);

        if (collectionRoute is null)
        {
            throw new ArgumentException(
                "Collection route does not exist, is inactive, or does not belong to this tenant.");
        }

        var schedules =
            await _scheduleRepository.GetActiveByRouteAsync(
                collectionRouteId,
                cancellationToken);

        return schedules
            .Select(Map)
            .ToList();
    }

    public async Task<CollectionRouteScheduleResponse?>
        GetByIdAsync(
            Guid id,
            Guid tenantId,
            Guid collectionRouteId,
            CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty ||
            tenantId == Guid.Empty ||
            collectionRouteId == Guid.Empty)
        {
            return null;
        }

        var collectionRoute =
            await _collectionRouteRepository.GetByIdAsync(
                collectionRouteId,
                tenantId,
                cancellationToken);

        if (collectionRoute is null)
        {
            return null;
        }

        var schedule =
            await _scheduleRepository.GetByIdAsync(
                id,
                collectionRouteId,
                cancellationToken);

        return schedule is null
            ? null
            : Map(schedule);
    }

    public async Task<CollectionRouteScheduleResponse?>
        UpdateAsync(
            Guid id,
            Guid tenantId,
            Guid collectionRouteId,
            UpdateCollectionRouteScheduleRequest request,
            CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        ValidateIdentifiers(
            tenantId,
            collectionRouteId);

        ValidateDayOfWeek(
            request.DayOfWeek);

        ValidateTimeRange(
            request.StartTime,
            request.EndTime);

        var collectionRoute =
            await _collectionRouteRepository.GetByIdAsync(
                collectionRouteId,
                tenantId,
                cancellationToken);

        if (collectionRoute is null)
        {
            return null;
        }

        var schedule =
            await _scheduleRepository
                .GetByIdIncludingInactiveAsync(
                    id,
                    collectionRouteId,
                    cancellationToken);

        if (schedule is null ||
            !schedule.IsActive)
        {
            return null;
        }

        var dayExists =
            await _scheduleRepository.DayExistsAsync(
                collectionRouteId,
                request.DayOfWeek,
                excludeId: id,
                cancellationToken: cancellationToken);

        if (dayExists)
        {
            throw new ArgumentException(
                "This collection day is already configured for the route.");
        }

        schedule.DayOfWeek =
            request.DayOfWeek;

        schedule.StartTime =
            request.StartTime;

        schedule.EndTime =
            request.EndTime;

        schedule.UpdatedAt =
            DateTime.UtcNow;

        await _scheduleRepository.SaveChangesAsync(
            cancellationToken);

        return Map(schedule);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid tenantId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty ||
            tenantId == Guid.Empty ||
            collectionRouteId == Guid.Empty)
        {
            return false;
        }

        var collectionRoute =
            await _collectionRouteRepository.GetByIdAsync(
                collectionRouteId,
                tenantId,
                cancellationToken);

        if (collectionRoute is null)
        {
            return false;
        }

        var schedule =
            await _scheduleRepository
                .GetByIdIncludingInactiveAsync(
                    id,
                    collectionRouteId,
                    cancellationToken);

        if (schedule is null ||
            !schedule.IsActive)
        {
            return false;
        }

        schedule.IsActive = false;
        schedule.UpdatedAt =
            DateTime.UtcNow;

        await _scheduleRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<CollectionRouteScheduleResponse?>
        ReactivateAsync(
            Guid id,
            Guid tenantId,
            Guid collectionRouteId,
            CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty ||
            tenantId == Guid.Empty ||
            collectionRouteId == Guid.Empty)
        {
            return null;
        }

        var collectionRoute =
            await _collectionRouteRepository.GetByIdAsync(
                collectionRouteId,
                tenantId,
                cancellationToken);

        if (collectionRoute is null)
        {
            return null;
        }

        var schedule =
            await _scheduleRepository
                .GetByIdIncludingInactiveAsync(
                    id,
                    collectionRouteId,
                    cancellationToken);

        if (schedule is null)
        {
            return null;
        }

        if (!schedule.IsActive)
        {
            schedule.IsActive = true;
            schedule.UpdatedAt =
                DateTime.UtcNow;

            await _scheduleRepository.SaveChangesAsync(
                cancellationToken);
        }

        return Map(schedule);
    }

    private static void ValidateIdentifiers(
        Guid tenantId,
        Guid collectionRouteId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be a valid identifier.");
        }

        if (collectionRouteId == Guid.Empty)
        {
            throw new ArgumentException(
                "CollectionRouteId must be a valid identifier.");
        }
    }

    private static void ValidateDayOfWeek(
        CollectionWeekDay dayOfWeek)
    {
        if (!Enum.IsDefined(
                typeof(CollectionWeekDay),
                dayOfWeek))
        {
            throw new ArgumentException(
                "Invalid collection day.");
        }
    }

    private static void ValidateTimeRange(
        TimeOnly? startTime,
        TimeOnly? endTime)
    {
        if (startTime.HasValue !=
            endTime.HasValue)
        {
            throw new ArgumentException(
                "StartTime and EndTime must both be provided or both be null.");
        }

        if (startTime.HasValue &&
            endTime.HasValue &&
            endTime.Value <= startTime.Value)
        {
            throw new ArgumentException(
                "EndTime must be later than StartTime.");
        }
    }

    private static CollectionRouteScheduleResponse
        Map(
            CollectionRouteSchedule schedule)
    {
        return new CollectionRouteScheduleResponse
        {
            Id = schedule.Id,
            CollectionRouteId =
                schedule.CollectionRouteId,
            DayOfWeek =
                schedule.DayOfWeek,
            StartTime =
                schedule.StartTime,
            EndTime =
                schedule.EndTime,
            IsActive =
                schedule.IsActive,
            CreatedAt =
                schedule.CreatedAt,
            UpdatedAt =
                schedule.UpdatedAt
        };
    }
}