using Microsoft.EntityFrameworkCore;
using Sanes.Application.Loans.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Loans.Repositories;

public class LoanGuaranteeAttachmentRepository
    : ILoanGuaranteeAttachmentRepository
{
    private readonly SanesDbContext
        _dbContext;

    public LoanGuaranteeAttachmentRepository(
        SanesDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task AddAsync(
        LoanGuaranteeAttachment attachment,
        CancellationToken cancellationToken = default)
    {
        await _dbContext
            .LoanGuaranteeAttachments
            .AddAsync(
                attachment,
                cancellationToken);
    }

    public async Task<List<LoanGuaranteeAttachment>>
        GetByGuaranteeAsync(
            Guid tenantId,
            Guid loanGuaranteeId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .LoanGuaranteeAttachments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.LoanGuaranteeId ==
                    loanGuaranteeId &&
                !x.IsDeleted)
            .OrderByDescending(x =>
                x.CreatedAt)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<LoanGuaranteeAttachment?>
        GetByIdAsync(
            Guid tenantId,
            Guid loanGuaranteeId,
            Guid attachmentId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .LoanGuaranteeAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId ==
                        tenantId &&
                    x.LoanGuaranteeId ==
                        loanGuaranteeId &&
                    x.Id ==
                        attachmentId &&
                    !x.IsDeleted,
                cancellationToken);
    }

    public async Task<LoanGuaranteeAttachment?>
        GetByIdForUpdateAsync(
            Guid tenantId,
            Guid loanGuaranteeId,
            Guid attachmentId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .LoanGuaranteeAttachments
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId ==
                        tenantId &&
                    x.LoanGuaranteeId ==
                        loanGuaranteeId &&
                    x.Id ==
                        attachmentId &&
                    !x.IsDeleted,
                cancellationToken);
    }

    public async Task<int>
        CountActiveByGuaranteeAsync(
            Guid tenantId,
            Guid loanGuaranteeId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .LoanGuaranteeAttachments
            .AsNoTracking()
            .CountAsync(
                x =>
                    x.TenantId ==
                        tenantId &&
                    x.LoanGuaranteeId ==
                        loanGuaranteeId &&
                    !x.IsDeleted,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}