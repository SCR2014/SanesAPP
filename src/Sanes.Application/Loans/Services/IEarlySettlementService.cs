using Sanes.Application.Loans.DTOs;

namespace Sanes.Application.Loans.Services;

public interface IEarlySettlementService
{
    Task<EarlySettlementQuoteResponse?> QuoteAsync(
        Guid tenantId,
        Guid loanId,
        EarlySettlementQuoteRequest request,
        CancellationToken cancellationToken = default);

    Task<EarlySettlementResponse?> ExecuteAsync(
        Guid tenantId,
        Guid appUserId,
        Guid loanId,
        EarlySettlementExecuteRequest request,
        CancellationToken cancellationToken = default);

    Task<EarlySettlementResponse?> GetByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);
}