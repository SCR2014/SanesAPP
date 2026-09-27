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

    public async Task<PaymentReceiptListResponse>
        GetPagedAsync(
            Guid tenantId,
            PaymentReceiptListRequest request,
            Guid? collectedByAppUserId,
            CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be valid.",
                nameof(tenantId));
        }

        ArgumentNullException.ThrowIfNull(
            request);

        if (request.From.HasValue &&
            request.To.HasValue &&
            request.From.Value >
            request.To.Value)
        {
            throw new ArgumentException(
                "From date cannot be after To date.",
                nameof(request));
        }

        if (request.Page < 1)
        {
            throw new ArgumentException(
                "Page must be greater than zero.",
                nameof(request));
        }

        if (request.PageSize < 1 ||
            request.PageSize > 100)
        {
            throw new ArgumentException(
                "PageSize must be between 1 and 100.",
                nameof(request));
        }

        if (collectedByAppUserId.HasValue &&
            collectedByAppUserId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "CollectorId must be valid.",
                nameof(collectedByAppUserId));
        }

        var normalizedSearch =
            string.IsNullOrWhiteSpace(
                request.Search)
                ? null
                : request.Search.Trim();

        if (normalizedSearch?.Length > 150)
        {
            throw new ArgumentException(
                "Search cannot exceed 150 characters.",
                nameof(request));
        }

        DateTime? fromInclusive =
            request.From?.ToDateTime(
                TimeOnly.MinValue,
                DateTimeKind.Utc);

        DateTime? toExclusive = null;

        if (request.To.HasValue)
        {
            if (request.To.Value ==
                DateOnly.MaxValue)
            {
                throw new ArgumentException(
                    "To date is outside the supported range.",
                    nameof(request));
            }

            toExclusive =
                request.To.Value
                    .AddDays(1)
                    .ToDateTime(
                        TimeOnly.MinValue,
                        DateTimeKind.Utc);
        }

        var skip =
            checked(
                (request.Page - 1) *
                request.PageSize);

        var result =
            await _paymentReceiptRepository
                .GetPagedAsync(
                    tenantId,
                    fromInclusive,
                    toExclusive,
                    normalizedSearch,
                    collectedByAppUserId,
                    skip,
                    request.PageSize,
                    cancellationToken);

        return new PaymentReceiptListResponse
        {
            Items =
                result.Items
                    .Select(Map)
                    .ToList(),

            Page =
                request.Page,

            PageSize =
                request.PageSize,

            TotalCount =
                result.TotalCount
        };
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