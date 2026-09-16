using Sanes.Application.LateFees.DTOs;

namespace Sanes.Application.LateFees.Services;

public interface ILateFeeAdministrationService
{
    Task<LateFeeLoanResponse?> GetByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<LateFeeChargeResponse?> AddAdjustmentAsync(
        Guid tenantId,
        Guid appUserId,
        Guid lateFeeChargeId,
        LateFeeAdjustmentRequest request,
        CancellationToken cancellationToken = default);
}