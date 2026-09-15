using Sanes.Domain.Entities;

namespace Sanes.Application.Payments.Repositories;

public interface IPaymentAllocationRepository
{
    Task AddAsync(
        PaymentAllocation allocation,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<PaymentAllocation> allocations,
        CancellationToken cancellationToken = default);

    Task<List<PaymentAllocation>> GetByPaymentAsync(
        Guid tenantId,
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalAppliedToLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, decimal>> GetTotalAppliedToLoansAsync(
        Guid tenantId,
        IEnumerable<Guid> loanIds,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalAppliedToLateFeeAsync(
        Guid tenantId,
        Guid lateFeeChargeId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, decimal>> GetTotalAppliedToLateFeesAsync(
        Guid tenantId,
        IEnumerable<Guid> lateFeeChargeIds,
        CancellationToken cancellationToken = default);

    Task<List<PaymentAllocation>> GetLoanBalanceAllocationsByLoansAsync(
        Guid tenantId,
        IEnumerable<Guid> loanIds,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}