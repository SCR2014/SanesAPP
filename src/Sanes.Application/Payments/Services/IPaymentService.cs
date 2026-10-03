using Sanes.Application.Payments.DTOs;
using Sanes.Application.Payments.Models;

namespace Sanes.Application.Payments.Services;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(
        Guid tenantId,
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse> CreateAsync(
        Guid tenantId,
        CreatePaymentRequest request,
        PaymentIdempotencyContext idempotencyContext,
        CancellationToken cancellationToken = default);

    Task<List<PaymentResponse>> GetAllByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse?> GetByIdAsync(
        Guid tenantId,
        Guid paymentId,
        CancellationToken cancellationToken = default);
}