using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Application.CollectionRoutes.Repositories;
using Sanes.Application.Tenants.Repositories;
using Sanes.Application.Clients.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.CollectionRoutes.Services;

public class CollectionRouteService : ICollectionRouteService
{
    private readonly ICollectionRouteRepository _collectionRouteRepository;

    private readonly IClientRepository _clientRepository;
    private readonly ITenantRepository _tenantRepository;

    public CollectionRouteService(
        ICollectionRouteRepository collectionRouteRepository,
        IClientRepository clientRepository,
        ITenantRepository tenantRepository)
    {
        _collectionRouteRepository = collectionRouteRepository;
        _clientRepository = clientRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<CollectionRouteResponse> CreateAsync(
        CreateCollectionRouteRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(
            request.TenantId,
            cancellationToken);

        if (tenant is null || !tenant.IsActive)
        {
            throw new InvalidOperationException(
                "Tenant does not exist or is inactive.");
        }

        var name = request.Name.Trim();
        var description = request.Description?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Name is required.");
        }

        if (!Enum.IsDefined(typeof(CollectionRouteOrderMode), request.OrderMode))
        {
            throw new ArgumentException("Invalid collection route order mode.");
        }

        var collectionRoute = new CollectionRoute
        {
            TenantId = request.TenantId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),

            OrderMode = request.OrderMode,

            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _collectionRouteRepository.AddAsync(
            collectionRoute,
            cancellationToken);

        await _collectionRouteRepository.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(collectionRoute);
    }

    public async Task<List<CollectionRouteResponse>> GetAllAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var routes = await _collectionRouteRepository.GetActiveByTenantAsync(
            tenantId,
            cancellationToken);

        return routes
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<CollectionRouteResponse?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var route = await _collectionRouteRepository.GetByIdAsync(
            id,
            tenantId,
            cancellationToken);

        if (route is null)
        {
            return null;
        }

        return MapToResponse(route);
    }

    public async Task<CollectionRouteResponse?> UpdateAsync(
        Guid id,
        Guid tenantId,
        UpdateCollectionRouteRequest request,
        CancellationToken cancellationToken = default)
    {
        var route =
            await _collectionRouteRepository.GetByIdIncludingInactiveAsync(
                id,
                tenantId,
                cancellationToken);

        if (route is null || !route.IsActive)
        {
            return null;
        }

        var name = request.Name.Trim();
        var description = request.Description?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Name is required.");
        }

        if (request.OrderMode.HasValue &&
            !Enum.IsDefined(typeof(CollectionRouteOrderMode), request.OrderMode.Value))
        {
            throw new ArgumentException("Invalid collection route order mode.");
        }

        route.Name = name;
        route.Description = description;
        if (request.OrderMode.HasValue)
        {
            route.OrderMode = request.OrderMode.Value;
        }
        route.UpdatedAt = DateTime.UtcNow;

        await _collectionRouteRepository.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(route);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var route =
            await _collectionRouteRepository.GetByIdIncludingInactiveAsync(
                id,
                tenantId,
                cancellationToken);

        if (route is null)
        {
            return false;
        }

        route.IsActive = false;
        route.UpdatedAt = DateTime.UtcNow;

        await _collectionRouteRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<CollectionRouteResponse?> ReactivateAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var route =
            await _collectionRouteRepository.GetByIdIncludingInactiveAsync(
                id,
                tenantId,
                cancellationToken);

        if (route is null)
        {
            return null;
        }

        route.IsActive = true;
        route.UpdatedAt = DateTime.UtcNow;

        await _collectionRouteRepository.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(route);
    }

    private static CollectionRouteResponse MapToResponse(
        CollectionRoute route)
    {
        return new CollectionRouteResponse
        {
            Id = route.Id,
            TenantId = route.TenantId,
            Name = route.Name,
            Description = route.Description,
            OrderMode = route.OrderMode,
            IsActive = route.IsActive,
            CreatedAt = route.CreatedAt,
            UpdatedAt = route.UpdatedAt
        };
    }

    public async Task<bool> ReorderClientsAsync(
    Guid id,
    Guid tenantId,
    ReorderCollectionRouteRequest request,
    CancellationToken cancellationToken = default)
    {
        var collectionRoute =
            await _collectionRouteRepository.GetByIdAsync(
                id,
                tenantId,
                cancellationToken);

        if (collectionRoute is null)
        {
            return false;
        }

        if (collectionRoute.OrderMode != CollectionRouteOrderMode.Manual)
        {
            throw new InvalidOperationException(
                "Clients can only be manually reordered when the collection route is in Manual order mode.");
        }

        var routeClients =
            await _clientRepository.GetByCollectionRouteForUpdateAsync(
                tenantId,
                id,
                cancellationToken);

        var requestedClientIds =
            request.ClientIds ?? new List<Guid>();

        if (requestedClientIds.Count != requestedClientIds.Distinct().Count())
        {
            throw new ArgumentException(
                "The client list contains duplicate identifiers.");
        }

        var routeClientIds = routeClients
            .Select(x => x.Id)
            .ToHashSet();

        if (requestedClientIds.Any(
                clientId => !routeClientIds.Contains(clientId)))
        {
            throw new ArgumentException(
                "One or more clients do not belong to this collection route.");
        }

        var clientsById = routeClients
            .ToDictionary(x => x.Id);

        for (var index = 0; index < requestedClientIds.Count; index++)
        {
            var client = clientsById[requestedClientIds[index]];

            client.CollectionRouteOrder = index + 1;
            client.UpdatedAt = DateTime.UtcNow;
        }

        var requestedClientIdSet =
            requestedClientIds.ToHashSet();

        foreach (var client in routeClients.Where(
                    x => !requestedClientIdSet.Contains(x.Id)))
        {
            client.CollectionRouteOrder = null;
            client.UpdatedAt = DateTime.UtcNow;
        }

        await _clientRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> OptimizeClientsAsync(
        Guid id,
        Guid tenantId,
        OptimizeCollectionRouteRequest request,
        CancellationToken cancellationToken = default)
    {
        var collectionRoute =
            await _collectionRouteRepository.GetByIdAsync(
                id,
                tenantId,
                cancellationToken);

        if (collectionRoute is null)
        {
            return false;
        }

        if (collectionRoute.OrderMode != CollectionRouteOrderMode.Automatic)
        {
            throw new InvalidOperationException(
                "Clients can only be automatically optimized when the collection route is in Automatic order mode.");
        }

        var hasStartLatitude = request.StartLatitude.HasValue;
        var hasStartLongitude = request.StartLongitude.HasValue;

        if (hasStartLatitude != hasStartLongitude)
        {
            throw new ArgumentException(
                "StartLatitude and StartLongitude must be provided together.");
        }

        if (request.StartLatitude.HasValue &&
            !IsValidLatitude(request.StartLatitude.Value))
        {
            throw new ArgumentException(
                "StartLatitude must be between -90 and 90.");
        }

        if (request.StartLongitude.HasValue &&
            !IsValidLongitude(request.StartLongitude.Value))
        {
            throw new ArgumentException(
                "StartLongitude must be between -180 and 180.");
        }

        var routeClients =
            await _clientRepository.GetByCollectionRouteForUpdateAsync(
                tenantId,
                id,
                cancellationToken);

        var clientsWithCoordinates = routeClients
            .Where(x =>
                x.Latitude.HasValue &&
                x.Longitude.HasValue &&
                IsValidLatitude(x.Latitude.Value) &&
                IsValidLongitude(x.Longitude.Value))
            .ToList();

        var clientsWithoutCoordinates = routeClients
            .Where(x =>
                !x.Latitude.HasValue ||
                !x.Longitude.HasValue ||
                !IsValidLatitude(x.Latitude.Value) ||
                !IsValidLongitude(x.Longitude.Value))
            .ToList();

        var orderedClients = new List<Client>();

        if (clientsWithCoordinates.Count > 0)
        {
            var remainingClients =
                new List<Client>(clientsWithCoordinates);

            Client currentClient;

            if (request.StartLatitude.HasValue &&
                request.StartLongitude.HasValue)
            {
                currentClient = remainingClients
                    .OrderBy(x => CalculateDistanceKm(
                        request.StartLatitude.Value,
                        request.StartLongitude.Value,
                        x.Latitude!.Value,
                        x.Longitude!.Value))
                    .ThenBy(x => x.Id)
                    .First();
            }
            else
            {
                currentClient = remainingClients
                    .OrderBy(x => x.Id)
                    .First();
            }

            orderedClients.Add(currentClient);
            remainingClients.Remove(currentClient);

            while (remainingClients.Count > 0)
            {
                var nextClient = remainingClients
                    .OrderBy(x => CalculateDistanceKm(
                        currentClient.Latitude!.Value,
                        currentClient.Longitude!.Value,
                        x.Latitude!.Value,
                        x.Longitude!.Value))
                    .ThenBy(x => x.Id)
                    .First();

                orderedClients.Add(nextClient);
                remainingClients.Remove(nextClient);

                currentClient = nextClient;
            }
        }

        for (var index = 0; index < orderedClients.Count; index++)
        {
            orderedClients[index].CollectionRouteOrder = index + 1;
            orderedClients[index].UpdatedAt = DateTime.UtcNow;
        }

        foreach (var client in clientsWithoutCoordinates)
        {
            client.CollectionRouteOrder = null;
            client.UpdatedAt = DateTime.UtcNow;
        }

        await _clientRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static bool IsValidLatitude(decimal latitude)
    {
        return latitude >= -90m && latitude <= 90m;
    }

    private static bool IsValidLongitude(decimal longitude)
    {
        return longitude >= -180m && longitude <= 180m;
    }

    private static double CalculateDistanceKm(
        decimal latitude1,
        decimal longitude1,
        decimal latitude2,
        decimal longitude2)
    {
        const double earthRadiusKm = 6371.0;

        var lat1 = DegreesToRadians((double)latitude1);
        var lon1 = DegreesToRadians((double)longitude1);
        var lat2 = DegreesToRadians((double)latitude2);
        var lon2 = DegreesToRadians((double)longitude2);

        var deltaLatitude = lat2 - lat1;
        var deltaLongitude = lon2 - lon1;

        var a =
            Math.Pow(Math.Sin(deltaLatitude / 2), 2) +
            Math.Cos(lat1) *
            Math.Cos(lat2) *
            Math.Pow(Math.Sin(deltaLongitude / 2), 2);

        var c = 2 * Math.Atan2(
            Math.Sqrt(a),
            Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}