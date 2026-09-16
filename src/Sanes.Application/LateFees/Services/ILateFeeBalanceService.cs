using Sanes.Application.LateFees.DTOs;

namespace Sanes.Application.LateFees.Services;

public interface ILateFeeBalanceService
{
    Task<List<OutstandingLateFeeItem>> GetOutstandingByLoanAsync(
        Guid tenantId,
        Guid loanId,
        DateTime? effectiveDateCutoff = null,
        CancellationToken cancellationToken = default);

    Task<decimal> GetOutstandingBalanceAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, decimal>> GetOutstandingBalancesByLoansAsync(
        Guid tenantId,
        IEnumerable<Guid> loanIds,
        CancellationToken cancellationToken = default);
}