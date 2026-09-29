using Sanes.Application.AppUsers.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Web.AppUsers;

public interface IAppUsersWebService
{
    Task<List<AppUserResponse>> GetAllAsync(
        AppUserRole? role = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<AppUserResponse?> GetByIdAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<AppUserResponse> CreateAsync(
        CreateAppUserRequest request,
        CancellationToken cancellationToken = default);

    Task<AppUserResponse?> UpdateAsync(
        Guid appUserId,
        UpdateAppUserRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<AppUserResponse?> ReactivateAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<List<AppUserCollectionRouteResponse>?> GetCollectionRoutesAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<bool> AssignCollectionRouteAsync(
        Guid appUserId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);

    Task<bool> UnassignCollectionRouteAsync(
        Guid appUserId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);

    Task<bool> SetPasswordAsync(
        Guid appUserId,
        string password,
        CancellationToken cancellationToken = default);
}