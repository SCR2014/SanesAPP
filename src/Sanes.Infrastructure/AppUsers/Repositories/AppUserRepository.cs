using Microsoft.EntityFrameworkCore;
using Sanes.Application.AppUsers.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;
using Sanes.Domain.Enums;

namespace Sanes.Infrastructure.AppUsers.Repositories;

public class AppUserRepository : IAppUserRepository
{
    private readonly SanesDbContext _dbContext;

    public AppUserRepository(SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AppUser?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AppUsers
            .FirstOrDefaultAsync(
                x =>
                    x.Id == id &&
                    x.TenantId == tenantId &&
                    x.IsActive,
                cancellationToken);
    }

    public async Task<AppUser?> GetByIdIncludingInactiveAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AppUsers
            .FirstOrDefaultAsync(
                x =>
                    x.Id == id &&
                    x.TenantId == tenantId,
                cancellationToken);
    }

    public async Task<List<AppUser>> GetActiveByTenantAsync(
        Guid tenantId,
        AppUserRole? role = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AppUsers
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsActive);

        if (role.HasValue)
        {
            query = query.Where(x =>
                x.Role == role.Value);
        }

        return await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Username)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UsernameExistsAsync(
        Guid tenantId,
        string username,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AppUsers
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Username == username);

        if (excludeId.HasValue)
        {
            query = query.Where(x =>
                x.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(
        AppUser appUser,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.AppUsers
            .AddAsync(
                appUser,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}