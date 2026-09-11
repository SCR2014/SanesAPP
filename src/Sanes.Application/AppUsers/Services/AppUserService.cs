using System.Net.Mail;
using Sanes.Application.AppUsers.DTOs;
using Sanes.Application.AppUsers.Repositories;
using Sanes.Application.Tenants.Repositories;
using Sanes.Application.CollectionRoutes.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.AppUsers.Services;

public class AppUserService : IAppUserService
{
    private readonly IAppUserRepository _appUserRepository;
    private readonly ITenantRepository _tenantRepository;

    private readonly IAppUserCollectionRouteRepository
    _appUserCollectionRouteRepository;

    private readonly ICollectionRouteRepository
        _collectionRouteRepository;

    public AppUserService(
        IAppUserRepository appUserRepository,
        ITenantRepository tenantRepository,
        IAppUserCollectionRouteRepository appUserCollectionRouteRepository,
        ICollectionRouteRepository collectionRouteRepository)
    {
        _appUserRepository = appUserRepository;
        _tenantRepository = tenantRepository;
        _appUserCollectionRouteRepository =
        appUserCollectionRouteRepository;
    _collectionRouteRepository = collectionRouteRepository;
    }

    public async Task<AppUserResponse> CreateAsync(
        CreateAppUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name);
        ValidateUsername(request.Username);
        ValidateEmail(request.Email);
        ValidateRole(request.Role);

        if (request.TenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be a valid identifier.");
        }

        var tenant = await _tenantRepository.GetByIdAsync(
            request.TenantId,
            cancellationToken);

        if (tenant is null)
        {
            throw new ArgumentException(
                "Tenant does not exist or is inactive.");
        }

        var normalizedUsername =
            NormalizeUsername(request.Username);

        var usernameExists =
            await _appUserRepository.UsernameExistsAsync(
                request.TenantId,
                normalizedUsername,
                cancellationToken: cancellationToken);

        if (usernameExists)
        {
            throw new ArgumentException(
                "Username already exists for this tenant.");
        }

        var now = DateTime.UtcNow;

        var appUser = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Name = request.Name.Trim(),
            Username = normalizedUsername,
            Email = NormalizeOptional(request.Email),
            Phone = NormalizeOptional(request.Phone),
            Role = request.Role,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _appUserRepository.AddAsync(
            appUser,
            cancellationToken);

        await _appUserRepository.SaveChangesAsync(
            cancellationToken);

        return Map(appUser);
    }

    public async Task<List<AppUserResponse>> GetAllAsync(
        Guid tenantId,
        AppUserRole? role = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be a valid identifier.");
        }

        if (role.HasValue &&
            !Enum.IsDefined(
                typeof(AppUserRole),
                role.Value))
        {
            throw new ArgumentException(
                "Invalid app user role.");
        }

        var appUsers =
            await _appUserRepository.GetActiveByTenantAsync(
                tenantId,
                role,
                cancellationToken);

        return appUsers
            .Select(Map)
            .ToList();
    }

    public async Task<AppUserResponse?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty ||
            tenantId == Guid.Empty)
        {
            return null;
        }

        var appUser =
            await _appUserRepository.GetByIdAsync(
                id,
                tenantId,
                cancellationToken);

        return appUser is null
            ? null
            : Map(appUser);
    }

    public async Task<AppUserResponse?> UpdateAsync(
        Guid id,
        Guid tenantId,
        UpdateAppUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty ||
            tenantId == Guid.Empty)
        {
            return null;
        }

        ValidateName(request.Name);
        ValidateUsername(request.Username);
        ValidateEmail(request.Email);
        ValidateRole(request.Role);

        var appUser =
            await _appUserRepository.GetByIdIncludingInactiveAsync(
                id,
                tenantId,
                cancellationToken);

        if (appUser is null ||
            !appUser.IsActive)
        {
            return null;
        }

        if (appUser.Role == AppUserRole.Collector &&
            request.Role != AppUserRole.Collector)
        {
            var assignments =
                await _appUserCollectionRouteRepository.GetByAppUserAsync(
                    appUser.Id,
                    cancellationToken);

            if (assignments.Count > 0)
            {
                throw new InvalidOperationException(
                    "A collector with assigned collection routes cannot change role. Unassign the routes first.");
            }
        }

        var normalizedUsername =
            NormalizeUsername(request.Username);

        var usernameExists =
            await _appUserRepository.UsernameExistsAsync(
                tenantId,
                normalizedUsername,
                appUser.Id,
                cancellationToken);

        if (usernameExists)
        {
            throw new ArgumentException(
                "Username already exists for this tenant.");
        }

        appUser.Name = request.Name.Trim();
        appUser.Username = normalizedUsername;
        appUser.Email = NormalizeOptional(request.Email);
        appUser.Phone = NormalizeOptional(request.Phone);
        appUser.Role = request.Role;
        appUser.UpdatedAt = DateTime.UtcNow;

        await _appUserRepository.SaveChangesAsync(
            cancellationToken);

        return Map(appUser);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty ||
            tenantId == Guid.Empty)
        {
            return false;
        }

        var appUser =
            await _appUserRepository.GetByIdIncludingInactiveAsync(
                id,
                tenantId,
                cancellationToken);

        if (appUser is null ||
            !appUser.IsActive)
        {
            return false;
        }

        appUser.IsActive = false;
        appUser.UpdatedAt = DateTime.UtcNow;

        await _appUserRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<AppUserResponse?> ReactivateAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty ||
            tenantId == Guid.Empty)
        {
            return null;
        }

        var appUser =
            await _appUserRepository.GetByIdIncludingInactiveAsync(
                id,
                tenantId,
                cancellationToken);

        if (appUser is null)
        {
            return null;
        }

        if (!appUser.IsActive)
        {
            appUser.IsActive = true;
            appUser.UpdatedAt = DateTime.UtcNow;

            await _appUserRepository.SaveChangesAsync(
                cancellationToken);
        }

        return Map(appUser);
    }

    public async Task<List<AppUserCollectionRouteResponse>?>
        GetCollectionRoutesAsync(
            Guid appUserId,
            Guid tenantId,
            CancellationToken cancellationToken = default)
    {
        if (appUserId == Guid.Empty ||
            tenantId == Guid.Empty)
        {
            return null;
        }

        var appUser =
            await _appUserRepository.GetByIdAsync(
                appUserId,
                tenantId,
                cancellationToken);

        if (appUser is null)
        {
            return null;
        }

        var assignments =
            await _appUserCollectionRouteRepository
                .GetByAppUserAsync(
                    appUserId,
                    cancellationToken);

        return assignments
            .Select(x => new AppUserCollectionRouteResponse
            {
                CollectionRouteId = x.CollectionRouteId,
                CollectionRouteName =
                    x.CollectionRoute.Name,
                AssignedAt = x.AssignedAt
            })
            .ToList();
    }

    public async Task<bool> AssignCollectionRouteAsync(
        Guid appUserId,
        Guid collectionRouteId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (appUserId == Guid.Empty ||
            collectionRouteId == Guid.Empty ||
            tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Identifiers must be valid.");
        }

        var appUser =
            await _appUserRepository.GetByIdAsync(
                appUserId,
                tenantId,
                cancellationToken);

        if (appUser is null)
        {
            return false;
        }

        if (appUser.Role != AppUserRole.Collector)
        {
            throw new InvalidOperationException(
                "Only users with Collector role can be assigned collection routes.");
        }

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

        var alreadyAssigned =
            await _appUserCollectionRouteRepository
                .ExistsAsync(
                    appUserId,
                    collectionRouteId,
                    cancellationToken);

        if (alreadyAssigned)
        {
            throw new ArgumentException(
                "Collection route is already assigned to this user.");
        }

        var assignment =
            new AppUserCollectionRoute
            {
                AppUserId = appUserId,
                CollectionRouteId = collectionRouteId,
                AssignedAt = DateTime.UtcNow
            };

        await _appUserCollectionRouteRepository
            .AddAsync(
                assignment,
                cancellationToken);

        await _appUserCollectionRouteRepository
            .SaveChangesAsync(
                cancellationToken);

        return true;
    }

    public async Task<bool> UnassignCollectionRouteAsync(
        Guid appUserId,
        Guid collectionRouteId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (appUserId == Guid.Empty ||
            collectionRouteId == Guid.Empty ||
            tenantId == Guid.Empty)
        {
            return false;
        }

        var appUser =
            await _appUserRepository.GetByIdAsync(
                appUserId,
                tenantId,
                cancellationToken);

        if (appUser is null)
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

        var assignment =
            await _appUserCollectionRouteRepository.GetAsync(
                appUserId,
                collectionRouteId,
                cancellationToken);

        if (assignment is null)
        {
            return false;
        }

        _appUserCollectionRouteRepository.Remove(
            assignment);

        await _appUserCollectionRouteRepository
            .SaveChangesAsync(
                cancellationToken);

        return true;
    }

    private static void ValidateName(
        string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Name is required.");
        }

        if (name.Trim().Length > 150)
        {
            throw new ArgumentException(
                "Name cannot exceed 150 characters.");
        }
    }

    private static void ValidateUsername(
        string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException(
                "Username is required.");
        }

        if (username.Trim().Length > 100)
        {
            throw new ArgumentException(
                "Username cannot exceed 100 characters.");
        }
    }

    private static void ValidateEmail(
        string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        if (email.Trim().Length > 150)
        {
            throw new ArgumentException(
                "Email cannot exceed 150 characters.");
        }

        try
        {
            _ = new MailAddress(email.Trim());
        }
        catch (FormatException)
        {
            throw new ArgumentException(
                "Email format is invalid.");
        }
    }

    private static void ValidateRole(
        AppUserRole role)
    {
        if (!Enum.IsDefined(
                typeof(AppUserRole),
                role))
        {
            throw new ArgumentException(
                "Invalid app user role.");
        }
    }

    private static string NormalizeUsername(
        string username)
    {
        return username
            .Trim()
            .ToLowerInvariant();
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static AppUserResponse Map(
        AppUser appUser)
    {
        return new AppUserResponse
        {
            Id = appUser.Id,
            TenantId = appUser.TenantId,
            Name = appUser.Name,
            Username = appUser.Username,
            Email = appUser.Email,
            Phone = appUser.Phone,
            Role = appUser.Role,
            IsActive = appUser.IsActive,
            CreatedAt = appUser.CreatedAt,
            UpdatedAt = appUser.UpdatedAt
        };
    }
}