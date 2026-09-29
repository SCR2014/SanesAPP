using Sanes.Application.Tenants.DTOs;

namespace Sanes.Web.TenantSettings;

public interface ITenantSettingsWebService
{
    Task<TenantDto?> GetCurrentAsync(
        CancellationToken cancellationToken = default);

    Task<TenantDto?> UpdateCurrentAsync(
        UpdateTenantRequest request,
        CancellationToken cancellationToken = default);
}