using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Authentication.Services;
using Sanes.Application.Payments.DTOs;
using Sanes.Application.Payments.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Authorize(
    Roles =
        nameof(AppUserRole.Administrator) +
        "," +
        nameof(AppUserRole.Collector))]
public class PaymentReceiptsController
    : ControllerBase
{
    private readonly IPaymentReceiptService
        _paymentReceiptService;

    private readonly ICurrentUserService
        _currentUserService;

    public PaymentReceiptsController(
        IPaymentReceiptService paymentReceiptService,
        ICurrentUserService currentUserService)
    {
        _paymentReceiptService =
            paymentReceiptService;

        _currentUserService =
            currentUserService;
    }

    // ============================================================
    // LIST
    // ============================================================

    [HttpGet(
        "api/payment-receipts")]
    [ProducesResponseType(
        typeof(PaymentReceiptListResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<
        ActionResult<PaymentReceiptListResponse>>
        GetAll(
            [FromQuery]
            PaymentReceiptListRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            var collectedByAppUserId =
                _currentUserService.Role ==
                AppUserRole.Collector
                    ? _currentUserService.AppUserId
                    : request.CollectorId;

            var result =
                await _paymentReceiptService
                    .GetPagedAsync(
                        _currentUserService.TenantId,
                        request,
                        collectedByAppUserId,
                        cancellationToken);

            return Ok(
                result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
        catch (OverflowException)
        {
            return BadRequest(
                new
                {
                    message =
                        "The requested page is outside the supported range."
                });
        }
    }

    // ============================================================
    // BY PAYMENT
    // ============================================================

    [HttpGet(
        "api/payments/{paymentId:guid}/receipt")]
    [ProducesResponseType(
        typeof(PaymentReceiptResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<PaymentReceiptResponse>>
        GetByPayment(
            Guid paymentId,
            CancellationToken cancellationToken)
    {
        var receipt =
            await _paymentReceiptService
                .GetByPaymentAsync(
                    _currentUserService.TenantId,
                    paymentId,
                    cancellationToken);

        if (receipt is null ||
            !CanReadReceipt(receipt))
        {
            return NotFound();
        }

        return Ok(
            receipt);
    }

    // ============================================================
    // BY RECEIPT ID
    // ============================================================

    [HttpGet(
        "api/payment-receipts/{receiptId:guid}")]
    [ProducesResponseType(
        typeof(PaymentReceiptResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<PaymentReceiptResponse>>
        GetById(
            Guid receiptId,
            CancellationToken cancellationToken)
    {
        var receipt =
            await _paymentReceiptService
                .GetByIdAsync(
                    _currentUserService.TenantId,
                    receiptId,
                    cancellationToken);

        if (receipt is null ||
            !CanReadReceipt(receipt))
        {
            return NotFound();
        }

        return Ok(
            receipt);
    }

    // ============================================================
    // BY HUMAN-READABLE NUMBER
    // ============================================================

    [HttpGet(
        "api/payment-receipts/by-number/{receiptNumber}")]
    [ProducesResponseType(
        typeof(PaymentReceiptResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<PaymentReceiptResponse>>
        GetByNumber(
            string receiptNumber,
            CancellationToken cancellationToken)
    {
        var receipt =
            await _paymentReceiptService
                .GetByNumberAsync(
                    _currentUserService.TenantId,
                    receiptNumber,
                    cancellationToken);

        if (receipt is null ||
            !CanReadReceipt(receipt))
        {
            return NotFound();
        }

        return Ok(
            receipt);
    }

    // ============================================================
    // AUTHORIZATION
    // ============================================================

    private bool CanReadReceipt(
        PaymentReceiptResponse receipt)
    {
        if (_currentUserService.Role ==
            AppUserRole.Administrator)
        {
            return true;
        }

        return
            receipt.CollectedByAppUserId.HasValue &&
            receipt.CollectedByAppUserId.Value ==
            _currentUserService.AppUserId;
    }
}