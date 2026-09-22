using Sanes.Application.Investors.DTOs;

namespace Sanes.Web.Investors;

public interface IInvestorsWebService
{
    Task<List<InvestorResponse>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<InvestorResponse> CreateAsync(
        CreateInvestorRequest request,
        CancellationToken cancellationToken = default);

    Task<InvestorResponse?> UpdateAsync(
        Guid investorId,
        UpdateInvestorRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid investorId,
        CancellationToken cancellationToken = default);

    Task<bool> ReactivateAsync(
        Guid investorId,
        CancellationToken cancellationToken = default);
}