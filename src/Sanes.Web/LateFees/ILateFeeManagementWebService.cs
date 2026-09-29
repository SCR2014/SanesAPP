using Sanes.Application.LateFees.DTOs;

namespace Sanes.Web.LateFees;

public interface ILateFeeManagementWebService
{
    Task<LateFeeLoanResponse?> GetByLoanAsync(
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<LateFeeChargeResponse?> AddAdjustmentAsync(
        Guid chargeId,
        LateFeeAdjustmentRequest request,
        CancellationToken cancellationToken = default);
}