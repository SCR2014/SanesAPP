using Sanes.Application.Payments.DTOs;
using Sanes.Application.Payments.Repositories;
using Sanes.Domain.Entities;

namespace Sanes.Application.Payments.Services;

public class PaymentReceiptService
    : IPaymentReceiptService
{
    private readonly IPaymentReceiptRepository
        _paymentReceiptRepository;

    public PaymentReceiptService(
        IPaymentReceiptRepository paymentReceiptRepository)
    {
        _paymentReceiptRepository =
            paymentReceiptRepository;
    }

    public async Task<PaymentReceiptResponse?>
        GetByPaymentAsync(
            Guid tenantId,
            Guid paymentId,
            CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be valid.",
                nameof(tenantId));
        }

        if (paymentId == Guid.Empty)
        {
            throw new ArgumentException(
                "PaymentId must be valid.",
                nameof(paymentId));
        }

        var receipt =
            await _paymentReceiptRepository
                .GetByPaymentAsync(
                    tenantId,
                    paymentId,
                    cancellationToken);

        return receipt is null
            ? null
            : Map(receipt);
    }

    public async Task<PaymentReceiptResponse?>
        GetByIdAsync(
            Guid tenantId,
            Guid receiptId,
            CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be valid.",
                nameof(tenantId));
        }

        if (receiptId == Guid.Empty)
        {
            throw new ArgumentException(
                "ReceiptId must be valid.",
                nameof(receiptId));
        }

        var receipt =
            await _paymentReceiptRepository
                .GetByIdAsync(
                    tenantId,
                    receiptId,
                    cancellationToken);

        return receipt is null
            ? null
            : Map(receipt);
    }

    public async Task<PaymentReceiptResponse?>
        GetByNumberAsync(
            Guid tenantId,
            string receiptNumber,
            CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be valid.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(
                receiptNumber))
        {
            throw new ArgumentException(
                "Receipt number is required.",
                nameof(receiptNumber));
        }

        var normalizedReceiptNumber =
            receiptNumber
                .Trim()
                .ToUpperInvariant();

        var receipt =
            await _paymentReceiptRepository
                .GetByNumberAsync(
                    tenantId,
                    normalizedReceiptNumber,
                    cancellationToken);

        return receipt is null
            ? null
            : Map(receipt);
    }

    private static PaymentReceiptResponse Map(
        PaymentReceipt receipt)
    {
        return new PaymentReceiptResponse
        {
            Id =
                receipt.Id,

            PaymentId =
                receipt.PaymentId,

            ReceiptNumber =
                receipt.ReceiptNumber,

            ReceiptYear =
                receipt.ReceiptYear,

            SequenceNumber =
                receipt.SequenceNumber,

            TenantName =
                receipt.TenantName,

            TenantLegalName =
                receipt.TenantLegalName,

            CurrencyCode =
                receipt.CurrencyCode,

            CurrencySymbol =
                receipt.CurrencySymbol,

            ClientId =
                receipt.ClientId,

            ClientName =
                receipt.ClientName,

            LoanId =
                receipt.LoanId,

            PaymentDate =
                receipt.PaymentDate,

            PaymentType =
                receipt.PaymentType,

            AmountReceived =
                receipt.AmountReceived,

            LateFeeAmountApplied =
                receipt.LateFeeAmountApplied,

            LoanBalanceAmountApplied =
                receipt.LoanBalanceAmountApplied,

            ContractualBalanceAfter =
                receipt.ContractualBalanceAfter,

            LateFeeBalanceAfter =
                receipt.LateFeeBalanceAfter,

            TotalOutstandingAfter =
                receipt.TotalOutstandingAfter,

            CollectedByAppUserId =
                receipt.CollectedByAppUserId,

            CollectedByName =
                receipt.CollectedByName,

            Notes =
                receipt.Notes,

            CreatedAt =
                receipt.CreatedAt
        };
    }
}