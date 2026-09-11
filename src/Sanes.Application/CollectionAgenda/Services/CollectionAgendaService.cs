using Sanes.Application.CollectionAgenda.DTOs;
using Sanes.Application.CollectionAgenda.Repositories;
using Sanes.Application.CollectionAgenda.Services;
using Sanes.Application.AppUsers.Repositories;
using Sanes.Domain.Enums;

namespace Sanes.Application.CollectionAgenda.Services;

public class CollectionAgendaService : ICollectionAgendaService
{
    private readonly ICollectionAgendaRepository _agendaRepository;
    private readonly IAppUserRepository _appUserRepository;

    public CollectionAgendaService(
        ICollectionAgendaRepository agendaRepository,
        IAppUserRepository appUserRepository)
    {
        _agendaRepository = agendaRepository;
        _appUserRepository = appUserRepository;
    }

    public async Task<CollectionAgendaResponse> GetDailyAgendaAsync(
        Guid tenantId,
        DateOnly date,
        Guid? appUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be a valid identifier.");
        }

        if (appUserId.HasValue &&
            appUserId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "AppUserId must be a valid identifier.");
        }

        if (appUserId.HasValue)
        {
            var appUser =
                await _appUserRepository.GetByIdAsync(
                    appUserId.Value,
                    tenantId,
                    cancellationToken);

            if (appUser is null)
            {
                throw new ArgumentException(
                    "App user does not exist, is inactive, or does not belong to this tenant.");
            }

            if (appUser.Role != AppUserRole.Collector)
            {
                throw new ArgumentException(
                    "The selected app user is not a collector.");
            }
        }

        var collectionWeekDay =
            MapDayOfWeek(date.DayOfWeek);

        var schedules =
            await _agendaRepository.GetSchedulesForDayAsync(
                tenantId,
                collectionWeekDay,
                cancellationToken);

        if (schedules.Count == 0)
        {
            return new CollectionAgendaResponse
            {
                Date = date,
                DayOfWeek = collectionWeekDay,
                RoutesCount = 0,
                CollectorsCount = 0,
                ClientsCount = 0,
                Routes = new List<CollectionAgendaRouteResponse>()
            };
        }

        var allRouteIds =
            schedules
                .Select(x => x.CollectionRouteId)
                .Distinct()
                .ToList();

        var collectors =
            await _agendaRepository.GetCollectorsForRoutesAsync(
                allRouteIds,
                tenantId,
                appUserId,
                cancellationToken);

        List<Guid> effectiveRouteIds;

        if (appUserId.HasValue)
        {
            effectiveRouteIds =
                collectors
                    .Select(x => x.CollectionRouteId)
                    .Distinct()
                    .ToList();
        }
        else
        {
            effectiveRouteIds = allRouteIds;
        }

        if (effectiveRouteIds.Count == 0)
        {
            return new CollectionAgendaResponse
            {
                Date = date,
                DayOfWeek = collectionWeekDay,
                RoutesCount = 0,
                CollectorsCount = 0,
                ClientsCount = 0,
                Routes = new List<CollectionAgendaRouteResponse>()
            };
        }

        var clients =
            await _agendaRepository.GetClientsForRoutesAsync(
                effectiveRouteIds,
                tenantId,
                cancellationToken);

        var effectiveSchedules =
            schedules
                .Where(x =>
                    effectiveRouteIds.Contains(
                        x.CollectionRouteId))
                .ToList();

        var routes =
            effectiveSchedules
                .Select(schedule =>
                {
                    var routeCollectors =
                        collectors
                            .Where(x =>
                                x.CollectionRouteId ==
                                schedule.CollectionRouteId)
                            .Select(x =>
                                new CollectionAgendaCollectorResponse
                                {
                                    AppUserId =
                                        x.AppUserId,
                                    Name =
                                        x.AppUser.Name,
                                    Username =
                                        x.AppUser.Username
                                })
                            .ToList();

                    var routeClients =
                        clients
                            .Where(x =>
                                x.CollectionRouteId ==
                                schedule.CollectionRouteId)
                            .Select(x =>
                                new CollectionAgendaClientResponse
                                {
                                    ClientId =
                                        x.Id,
                                    FirstName =
                                        x.FirstName,
                                    LastName =
                                        x.LastName,
                                    Phone =
                                        x.Phone,
                                    Address =
                                        x.Address,
                                    CollectionRouteOrder =
                                        x.CollectionRouteOrder
                                })
                            .ToList();

                    return new CollectionAgendaRouteResponse
                    {
                        CollectionRouteId =
                            schedule.CollectionRouteId,

                        CollectionRouteName =
                            schedule.CollectionRoute.Name,

                        StartTime =
                            schedule.StartTime,

                        EndTime =
                            schedule.EndTime,

                        Collectors =
                            routeCollectors,

                        Clients =
                            routeClients
                    };
                })
                .ToList();

        return new CollectionAgendaResponse
        {
            Date =
                date,

            DayOfWeek =
                collectionWeekDay,

            RoutesCount =
                routes.Count,

            CollectorsCount =
                routes
                    .SelectMany(x => x.Collectors)
                    .Select(x => x.AppUserId)
                    .Distinct()
                    .Count(),

            ClientsCount =
                routes
                    .SelectMany(x => x.Clients)
                    .Select(x => x.ClientId)
                    .Distinct()
                    .Count(),

            Routes =
                routes
        };
    }

    private static CollectionWeekDay MapDayOfWeek(
        DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday =>
                CollectionWeekDay.Monday,

            DayOfWeek.Tuesday =>
                CollectionWeekDay.Tuesday,

            DayOfWeek.Wednesday =>
                CollectionWeekDay.Wednesday,

            DayOfWeek.Thursday =>
                CollectionWeekDay.Thursday,

            DayOfWeek.Friday =>
                CollectionWeekDay.Friday,

            DayOfWeek.Saturday =>
                CollectionWeekDay.Saturday,

            DayOfWeek.Sunday =>
                CollectionWeekDay.Sunday,

            _ => throw new ArgumentOutOfRangeException(
                nameof(dayOfWeek))
        };
    }
}