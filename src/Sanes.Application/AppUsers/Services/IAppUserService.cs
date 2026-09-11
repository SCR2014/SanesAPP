using Sanes.Application.AppUsers.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Application.AppUsers.Services;

public interface IAppUserService
{
    Task<AppUserResponse> CreateAsync(
        CreateAppUserRequest request,
        CancellationToken cancellationToken = default);

    Task<List<AppUserResponse>> GetAllAsync(
        Guid tenantId,
        AppUserRole? role = null,
        CancellationToken cancellationToken = default);

    Task<AppUserResponse?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<AppUserResponse?> UpdateAsync(
        Guid id,
        Guid tenantId,
        UpdateAppUserRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<AppUserResponse?> ReactivateAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<List<AppUserCollectionRouteResponse>?> GetCollectionRoutesAsync(
        Guid appUserId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> AssignCollectionRouteAsync(
        Guid appUserId,
        Guid collectionRouteId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> UnassignCollectionRouteAsync(
        Guid appUserId,
        Guid collectionRouteId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}