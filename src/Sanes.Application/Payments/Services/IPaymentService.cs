using Sanes.Application.Payments.DTOs;

namespace Sanes.Application.Payments.Services;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
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