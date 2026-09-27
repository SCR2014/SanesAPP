using Sanes.Application.Payments.DTOs;

namespace Sanes.Web.Printing;

public interface IReceiptPrinter
{
    Task PrintAsync(
        PaymentReceiptResponse receipt,
        CancellationToken cancellationToken = default);
}