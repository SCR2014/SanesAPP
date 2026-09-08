using Sanes.Application.Tenants.DTOs;
using Sanes.Application.Tenants.Repositories;
using Sanes.Domain.Entities;

namespace Sanes.Application.Tenants.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;

    public TenantService(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantDto> CreateAsync(
        CreateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var legalName = request.LegalName?.Trim();
        var phone = request.Phone?.Trim();
        var email = request.Email?.Trim();
        var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        var currencySymbol = request.CurrencySymbol.Trim();

        var tenant = new Tenant
        {
            Name = name,
            LegalName = legalName,
            Phone = phone,
            Email = email,
            CurrencyCode = currencyCode,
            CurrencySymbol = currencySymbol
        };

        await _tenantRepository.AddAsync(
            tenant,
            cancellationToken);

        await _tenantRepository.SaveChangesAsync(
            cancellationToken);

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            LegalName = tenant.LegalName,
            Phone = tenant.Phone,
            Email = tenant.Email,
            CurrencyCode = tenant.CurrencyCode,
            CurrencySymbol = tenant.CurrencySymbol,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };
    }

    public async Task<List<TenantDto>> GetAllAsync(
    CancellationToken cancellationToken = default)
{
    var tenants = await _tenantRepository.GetAllAsync(
        cancellationToken);

    return tenants
        .Select(tenant => new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            LegalName = tenant.LegalName,
            Phone = tenant.Phone,
            Email = tenant.Email,
            CurrencyCode = tenant.CurrencyCode,
            CurrencySymbol = tenant.CurrencySymbol,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        })
        .ToList();
}

    public async Task<TenantDto?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{
    var tenant = await _tenantRepository.GetByIdAsync(
        id,
        cancellationToken);

    if (tenant is null)
    {
        return null;
    }

    return new TenantDto
    {
        Id = tenant.Id,
        Name = tenant.Name,
        LegalName = tenant.LegalName,
        Phone = tenant.Phone,
        Email = tenant.Email,
        CurrencyCode = tenant.CurrencyCode,
        CurrencySymbol = tenant.CurrencySymbol,
        IsActive = tenant.IsActive,
        CreatedAt = tenant.CreatedAt,
        UpdatedAt = tenant.UpdatedAt
    };
}

public async Task<TenantDto?> UpdateAsync(
    Guid id,
    UpdateTenantRequest request,
    CancellationToken cancellationToken = default)
{
    var tenant = await _tenantRepository.GetByIdForUpdateAsync(
        id,
        cancellationToken);

    if (tenant is null || !tenant.IsActive)
    {
        return null;
    }

    tenant.Name = request.Name.Trim();
    tenant.LegalName = request.LegalName?.Trim();
    tenant.Phone = request.Phone?.Trim();
    tenant.Email = request.Email?.Trim();
    tenant.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
    tenant.CurrencySymbol = request.CurrencySymbol.Trim();
    tenant.UpdatedAt = DateTime.UtcNow;

    await _tenantRepository.SaveChangesAsync(
        cancellationToken);

    return new TenantDto
    {
        Id = tenant.Id,
        Name = tenant.Name,
        LegalName = tenant.LegalName,
        Phone = tenant.Phone,
        Email = tenant.Email,
        CurrencyCode = tenant.CurrencyCode,
        CurrencySymbol = tenant.CurrencySymbol,
        IsActive = tenant.IsActive,
        CreatedAt = tenant.CreatedAt,
        UpdatedAt = tenant.UpdatedAt
    };
}
    
    public async Task<bool> DeleteAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{
    var tenant = await _tenantRepository.GetByIdForUpdateAsync(
        id,
        cancellationToken);

    if (tenant is null)
    {
        return false;
    }

    tenant.IsActive = false;
    tenant.UpdatedAt = DateTime.UtcNow;

    await _tenantRepository.SaveChangesAsync(
        cancellationToken);

    return true;
}

    public async Task<bool> ReactivateAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{
    var tenant = await _tenantRepository.GetByIdForUpdateAsync(
        id,
        cancellationToken);

    if (tenant is null)
    {
        return false;
    }

    tenant.IsActive = true;
    tenant.UpdatedAt = DateTime.UtcNow;

    await _tenantRepository.SaveChangesAsync(
        cancellationToken);

    return true;
}
}