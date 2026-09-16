using Sanes.Application.Tenants.DTOs;
using Sanes.Application.Tenants.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

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
        ValidateLateFeePolicy(
            request.DefaultLateFeeEnabled,
            request.DefaultLateFeeCalculationType,
            request.DefaultLateFeeAmount,
            request.DefaultLateFeeGraceDays);

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
            CurrencySymbol = currencySymbol,

            DefaultLateFeeEnabled =
                request.DefaultLateFeeEnabled,

            DefaultLateFeeCalculationType =
                request.DefaultLateFeeCalculationType,

            DefaultLateFeeAmount =
                request.DefaultLateFeeAmount,

            DefaultLateFeeGraceDays =
                request.DefaultLateFeeGraceDays
        };

        await _tenantRepository.AddAsync(
            tenant,
            cancellationToken);

        await _tenantRepository.SaveChangesAsync(
            cancellationToken);

        return Map(tenant);
    }

    public async Task<List<TenantDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantRepository.GetAllAsync(
            cancellationToken);

        return tenants
            .Select(Map)
            .ToList();
    }

    public async Task<TenantDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(
            id,
            cancellationToken);

        return tenant is null
            ? null
            : Map(tenant);
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

        ValidateLateFeePolicy(
            request.DefaultLateFeeEnabled,
            request.DefaultLateFeeCalculationType,
            request.DefaultLateFeeAmount,
            request.DefaultLateFeeGraceDays);

        tenant.Name = request.Name.Trim();
        tenant.LegalName = request.LegalName?.Trim();
        tenant.Phone = request.Phone?.Trim();
        tenant.Email = request.Email?.Trim();
        tenant.CurrencyCode =
            request.CurrencyCode.Trim().ToUpperInvariant();
        tenant.CurrencySymbol =
            request.CurrencySymbol.Trim();

        tenant.DefaultLateFeeEnabled =
            request.DefaultLateFeeEnabled;

        tenant.DefaultLateFeeCalculationType =
            request.DefaultLateFeeCalculationType;

        tenant.DefaultLateFeeAmount =
            request.DefaultLateFeeAmount;

        tenant.DefaultLateFeeGraceDays =
            request.DefaultLateFeeGraceDays;

        tenant.UpdatedAt = DateTime.UtcNow;

        await _tenantRepository.SaveChangesAsync(
            cancellationToken);

        return Map(tenant);
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

    private static void ValidateLateFeePolicy(
        bool enabled,
        LateFeeCalculationType calculationType,
        decimal amount,
        int graceDays)
    {
        if (!Enum.IsDefined(
                typeof(LateFeeCalculationType),
                calculationType))
        {
            throw new InvalidOperationException(
                "Late fee calculation type is invalid.");
        }

        if (
            calculationType !=
            LateFeeCalculationType.FixedAmountPerInstallment)
        {
            throw new InvalidOperationException(
                "Only fixed late fees are currently supported.");
        }

        if (amount < 0)
        {
            throw new InvalidOperationException(
                "Late fee amount cannot be negative.");
        }

        if (graceDays < 0)
        {
            throw new InvalidOperationException(
                "Late fee grace days cannot be negative.");
        }

        if (enabled && amount <= 0)
        {
            throw new InvalidOperationException(
                "Late fee amount must be greater than zero when late fees are enabled.");
        }
    }

    private static TenantDto Map(Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,

            Name = tenant.Name,
            LegalName = tenant.LegalName,

            Phone = tenant.Phone,
            Email = tenant.Email,

            CurrencyCode = tenant.CurrencyCode,
            CurrencySymbol = tenant.CurrencySymbol,

            DefaultLateFeeEnabled =
                tenant.DefaultLateFeeEnabled,

            DefaultLateFeeCalculationType =
                tenant.DefaultLateFeeCalculationType,

            DefaultLateFeeAmount =
                tenant.DefaultLateFeeAmount,

            DefaultLateFeeGraceDays =
                tenant.DefaultLateFeeGraceDays,

            IsActive = tenant.IsActive,

            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };
    }
}