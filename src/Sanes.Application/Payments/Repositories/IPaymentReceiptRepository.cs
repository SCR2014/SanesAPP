using Sanes.Domain.Entities;

namespace Sanes.Application.Payments.Repositories;

public interface IPaymentReceiptRepository
{
    Task AddAsync(
        PaymentReceipt receipt,
        CancellationToken cancellationToken = default);

    Task<PaymentReceipt?> GetByPaymentAsync(
        Guid tenantId,
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<PaymentReceipt?> GetByIdAsync(
        Guid tenantId,
        Guid receiptId,
        CancellationToken cancellationToken = default);

    Task<PaymentReceipt?> GetByNumberAsync(
        Guid tenantId,
        string receiptNumber,
        CancellationToken cancellationToken = default);

    Task<int> GetNextSequenceNumberAsync(
        Guid tenantId,
        int year,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}