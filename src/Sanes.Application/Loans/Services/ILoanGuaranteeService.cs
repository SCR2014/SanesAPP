using Sanes.Application.Loans.DTOs;

namespace Sanes.Application.Loans.Services;

public interface ILoanGuaranteeService
{
    Task<LoanGuaranteeResponse?> GetByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<LoanGuaranteeResponse?> CreateAsync(
        Guid tenantId,
        Guid loanId,
        CreateLoanGuaranteeRequest request,
        CancellationToken cancellationToken = default);

    Task<LoanGuaranteeResponse?> UpdateAsync(
        Guid tenantId,
        Guid loanId,
        UpdateLoanGuaranteeRequest request,
        CancellationToken cancellationToken = default);
}