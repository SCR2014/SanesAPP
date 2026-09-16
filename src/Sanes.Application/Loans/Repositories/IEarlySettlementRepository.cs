using Sanes.Domain.Entities;

namespace Sanes.Application.Loans.Repositories;

public interface IEarlySettlementRepository
{
    Task<EarlySettlement?> GetByIdAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<EarlySettlement?> GetByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        EarlySettlement earlySettlement,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}