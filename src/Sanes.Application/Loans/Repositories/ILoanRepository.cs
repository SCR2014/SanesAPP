using Sanes.Domain.Entities;

namespace Sanes.Application.Loans.Repositories;

public interface ILoanRepository
{
    Task AddAsync(
        Loan loan,
        CancellationToken cancellationToken = default);

    Task<List<Loan>> GetAllAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Loan?> GetByIdAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<Loan?> GetByIdForUpdateAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<List<Loan>> GetActiveByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}