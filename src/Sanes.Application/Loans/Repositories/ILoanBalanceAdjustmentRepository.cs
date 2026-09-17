using Sanes.Domain.Entities;

namespace Sanes.Application.Loans.Repositories;

public interface ILoanBalanceAdjustmentRepository
{
    Task AddAsync(
        LoanBalanceAdjustment adjustment,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalReductionsByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, decimal>> GetTotalReductionsByLoansAsync(
        Guid tenantId,
        IEnumerable<Guid> loanIds,
        CancellationToken cancellationToken = default);

    Task<List<LoanBalanceAdjustment>> GetByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}