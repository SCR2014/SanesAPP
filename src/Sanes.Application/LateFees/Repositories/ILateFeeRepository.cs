using Sanes.Domain.Entities;

namespace Sanes.Application.LateFees.Repositories;

public interface ILateFeeRepository
{
    Task AddChargeAsync(
        LateFeeCharge charge,
        CancellationToken cancellationToken = default);

    Task AddAdjustmentAsync(
        LateFeeAdjustment adjustment,
        CancellationToken cancellationToken = default);

    Task<List<LateFeeCharge>> GetByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<List<LateFeeCharge>> GetByLoansAsync(
        Guid tenantId,
        IEnumerable<Guid> loanIds,
        CancellationToken cancellationToken = default);

    Task<LateFeeCharge?> GetChargeByIdAsync(
        Guid tenantId,
        Guid chargeId,
        CancellationToken cancellationToken = default);

    Task<LateFeeCharge?> GetChargeForUpdateAsync(
        Guid tenantId,
        Guid chargeId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid tenantId,
        Guid loanId,
        int installmentNumber,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}