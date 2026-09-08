using Sanes.Application.Investors.DTOs;

namespace Sanes.Application.Investors.Services;

public interface IInvestorService
{
    Task<InvestorResponse> CreateAsync(
        CreateInvestorRequest request,
        CancellationToken cancellationToken = default);

    Task<List<InvestorResponse>> GetAllAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<InvestorResponse?> GetByIdAsync(
        Guid tenantId,
        Guid investorId,
        CancellationToken cancellationToken = default);

    Task<InvestorResponse?> UpdateAsync(
        Guid tenantId,
        Guid investorId,
        UpdateInvestorRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid tenantId,
        Guid investorId,
        CancellationToken cancellationToken = default);

    Task<bool> ReactivateAsync(
        Guid tenantId,
        Guid investorId,
        CancellationToken cancellationToken = default);
}