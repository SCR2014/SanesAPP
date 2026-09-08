using Microsoft.EntityFrameworkCore;
using Sanes.Application.Loans.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Loans.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly SanesDbContext _dbContext;

    public LoanRepository(SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Loan loan,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Loans.AddAsync(
            loan,
            cancellationToken);
    }

    public async Task<List<Loan>> GetAllAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Loans
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Loan?> GetByIdAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Loans
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == loanId,
                cancellationToken);
    }

    public async Task<Loan?> GetByIdForUpdateAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Loans
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == loanId,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<List<Loan>> GetActiveByTenantAsync(
    Guid tenantId,
    CancellationToken cancellationToken = default)
{
    return await _dbContext.Loans
        .AsNoTracking()
        .Include(x => x.Client)
        .Where(x =>
            x.TenantId == tenantId &&
            x.Status == LoanStatus.Active)
        .OrderBy(x => x.NextPaymentDate)
        .ThenBy(x => x.CreatedAt)
        .ToListAsync(cancellationToken);
}
}