using Sanes.Application.Loans.DTOs;
using Sanes.Application.Tenants.DTOs;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Investors.DTOs;

namespace Sanes.Web.Loans;

public interface ILoansWebService
{
    Task<List<ClientResponse>> GetClientsAsync(
        CancellationToken cancellationToken = default);

    Task<List<InvestorResponse>> GetInvestorsAsync(
        CancellationToken cancellationToken = default);
    Task<List<LoanResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<LoanResponse?> GetByIdAsync(
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<LoanFinancialSummaryResponse?>
        GetFinancialSummaryAsync(
            Guid loanId,
            CancellationToken cancellationToken = default);

    Task<TenantDto> GetTenantAsync(
        CancellationToken cancellationToken = default);

    Task<LoanResponse> CreateAsync(
        CreateLoanRequest request,
        CancellationToken cancellationToken = default);

    Task<LoanResponse?> UpdateAsync(
        Guid loanId,
        UpdateLoanRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(
        Guid loanId,
        CancellationToken cancellationToken = default);
}