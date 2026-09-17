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

        if (receipt is null)
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

        if (receipt is null)
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

        if (receipt is null)
        {
            return NotFound();
        }

        return Ok(
            receipt);
    }
}