using Microsoft.EntityFrameworkCore;
using Sanes.Application.Authentication.Services;
using Sanes.Application.Provisioning.DTOs;
using Sanes.Application.Provisioning.Services;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Provisioning.Services;

public class ProvisioningService : IProvisioningService
{
    private readonly SanesDbContext _dbContext;
    private readonly IPasswordService _passwordService;

    public ProvisioningService(
        SanesDbContext dbContext,
        IPasswordService passwordService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
    }

    public async Task<ProvisionTenantResponse> ProvisionTenantAsync(
        ProvisionTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var now = DateTime.UtcNow;

        var normalizedUsername =
            request.Administrator.Username
                .Trim()
                .ToLowerInvariant();

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.Tenant.Name.Trim(),
                LegalName = NormalizeOptional(
                    request.Tenant.LegalName),
                Phone = NormalizeOptional(
                    request.Tenant.Phone),
                Email = NormalizeOptional(
                    request.Tenant.Email),
                CurrencyCode =
                    request.Tenant.CurrencyCode
                        .Trim()
                        .ToUpperInvariant(),
                CurrencySymbol =
                    request.Tenant.CurrencySymbol.Trim(),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var administrator = new AppUser
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = request.Administrator.Name.Trim(),
                Username = normalizedUsername,
                PasswordHash =
                    _passwordService.HashPassword(
                        request.Administrator.Password),
                Email = NormalizeOptional(
                    request.Administrator.Email),
                Phone = NormalizeOptional(
                    request.Administrator.Phone),
                Role = AppUserRole.Administrator,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _dbContext.Tenants.AddAsync(
                tenant,
                cancellationToken);

            await _dbContext.AppUsers.AddAsync(
                administrator,
                cancellationToken);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new ProvisionTenantResponse
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                AdministratorId = administrator.Id,
                AdministratorName = administrator.Name,
                Username = administrator.Username,
                CreatedAt = now
            };
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static void ValidateRequest(
        ProvisionTenantRequest request)
    {
        if (request.Tenant is null)
        {
            throw new ArgumentException(
                "Tenant information is required.");
        }

        if (request.Administrator is null)
        {
            throw new ArgumentException(
                "Administrator information is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Tenant.Name))
        {
            throw new ArgumentException(
                "Tenant name is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Tenant.CurrencyCode) ||
            request.Tenant.CurrencyCode.Trim().Length != 3)
        {
            throw new ArgumentException(
                "CurrencyCode must contain exactly 3 characters.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Tenant.CurrencySymbol))
        {
            throw new ArgumentException(
                "CurrencySymbol is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Administrator.Name))
        {
            throw new ArgumentException(
                "Administrator name is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Administrator.Username))
        {
            throw new ArgumentException(
                "Administrator username is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Administrator.Password) ||
            request.Administrator.Password.Length < 8 ||
            request.Administrator.Password.Length > 100)
        {
            throw new ArgumentException(
                "Administrator password must contain between 8 and 100 characters.");
        }
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}