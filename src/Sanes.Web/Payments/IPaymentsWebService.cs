using Sanes.Application.Payments.DTOs;

namespace Sanes.Web.Payments;

public interface IPaymentsWebService
{
    Task<List<PaymentResponse>> GetByLoanAsync(
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse?> GetByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);
}