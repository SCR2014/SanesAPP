using Sanes.Application.Loans.DTOs;

namespace Sanes.Application.Loans.Services;

public interface ILoanService
{
    Task<LoanResponse> CreateAsync(
        CreateLoanRequest request,
        CancellationToken cancellationToken = default);

    Task<List<LoanResponse>> GetAllAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<LoanResponse?> GetByIdAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<LoanResponse?> UpdateAsync(
        Guid tenantId,
        Guid loanId,
        UpdateLoanRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<LoanFinancialSummaryResponse?> GetFinancialSummaryAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<List<ActiveLoanPortfolioItemResponse>> GetActivePortfolioAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<ActivePortfolioSummaryResponse> GetActivePortfolioSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<List<CollectionLoanItemResponse>> GetCollectionPortfolioAsync(
        Guid tenantId,
        bool overdueOnly = false,
        DateTime? collectionDate = null,
        DateTime? dueDate = null,
        string? search = null,
        Guid? investorId = null,
        Guid? clientId = null,
        Guid? collectionRouteId = null,
        CancellationToken cancellationToken = default);

    Task<CollectionPortfolioSummaryResponse> GetCollectionPortfolioSummaryAsync(
        Guid tenantId,
        bool overdueOnly = false,
        DateTime? collectionDate = null,
        DateTime? dueDate = null,
        string? search = null,
        Guid? investorId = null,
        Guid? clientId = null,
        Guid? collectionRouteId = null,
        CancellationToken cancellationToken = default);
}