using Sanes.Application.Loans.DTOs;

namespace Sanes.Web.EarlySettlements;

public interface IEarlySettlementsWebService
{
    Task<EarlySettlementQuoteResponse?> QuoteAsync(
        Guid loanId,
        EarlySettlementQuoteRequest request,
        CancellationToken cancellationToken = default);

    Task<EarlySettlementResponse?> ExecuteAsync(
        Guid loanId,
        EarlySettlementExecuteRequest request,
        CancellationToken cancellationToken = default);

    Task<EarlySettlementResponse?> GetByLoanAsync(
        Guid loanId,
        CancellationToken cancellationToken = default);
}