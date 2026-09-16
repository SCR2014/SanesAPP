namespace Sanes.Application.LateFees.Services;

public interface ILateFeeAccrualService
{
    Task<int> AccrueForLoanAsync(
        Guid tenantId,
        Guid loanId,
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default);

    Task<int> AccrueForTenantAsync(
        Guid tenantId,
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default);
}