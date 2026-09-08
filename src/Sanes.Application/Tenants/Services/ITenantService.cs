using Sanes.Application.Tenants.DTOs;

namespace Sanes.Application.Tenants.Services;

public interface ITenantService
{
    Task<TenantDto> CreateAsync(
        CreateTenantRequest request,
        CancellationToken cancellationToken = default);

    Task<List<TenantDto>> GetAllAsync(
    CancellationToken cancellationToken = default);

    Task<TenantDto?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default);

    Task<TenantDto?> UpdateAsync(
    Guid id,
    UpdateTenantRequest request,
    CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
    Guid id,
    CancellationToken cancellationToken = default);

    Task<bool> ReactivateAsync(
    Guid id,
    CancellationToken cancellationToken = default);
}