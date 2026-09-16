using Sanes.Application.Provisioning.DTOs;

namespace Sanes.Application.Provisioning.Services;

public interface IProvisioningService
{
    Task<ProvisionTenantResponse> ProvisionTenantAsync(
        ProvisionTenantRequest request,
        CancellationToken cancellationToken = default);
}