using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Authentication.Services;
using Sanes.Application.Payments.DTOs;
using Sanes.Application.Payments.Services;
using Sanes.Domain.Enums;
using Sanes.Application.Payments.Exceptions;
using Sanes.Application.Payments.Models;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    private readonly IPaymentReversalService
        _paymentReversalService;

    private readonly ICurrentUserService
        _currentUserService;

    public PaymentsController(
        IPaymentService paymentService,
        IPaymentReversalService paymentReversalService,
        ICurrentUserService currentUserService)
    {
        _paymentService =
            paymentService;

        _paymentReversalService =
            paymentReversalService;

        _currentUserService =
            currentUserService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(PaymentResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaymentResponse>> Create(
        [FromBody] CreatePaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")]
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        /*
        * Todo cobro iniciado externamente debe tener una identidad
        * explícita de operación.
        *
        * No deduplicamos por monto, préstamo o fecha porque dos
        * pagos legítimos pueden compartir esos mismos valores.
        */
        if (string.IsNullOrWhiteSpace(
                idempotencyKey))
        {
            return BadRequest(
                new
                {
                    message =
                        "Idempotency-Key header is required."
                });
        }

        if (!Guid.TryParse(
                idempotencyKey.Trim(),
                out var parsedIdempotencyKey) ||
            parsedIdempotencyKey == Guid.Empty)
        {
            return BadRequest(
                new
                {
                    message =
                        "Idempotency-Key must be a valid non-empty GUID."
                });
        }

        /*
        * El fingerprint se calcula sobre el request semántico
        * antes de ejecutar cualquier efecto financiero.
        */
        var requestHash =
            PaymentIdempotencyFingerprint
                .CreateAdministrative(
                    request);

        var idempotencyContext =
            new PaymentIdempotencyContext(
                parsedIdempotencyKey,
                requestHash);

        try
        {
            var payment =
                await _paymentService.CreateAsync(
                    _currentUserService.TenantId,
                    request,
                    idempotencyContext,
                    cancellationToken);

            /*
            * Tanto la primera ejecución como un retry exitoso
            * devuelven la misma representación y la misma
            * Location del Payment original.
            */
            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = payment.Id
                },
                payment);
        }
        catch (PaymentIdempotencyConflictException ex)
        {
            /*
            * Misma key + payload diferente.
            *
            * 409 expresa que la solicitud individual puede ser
            * válida, pero entra en conflicto con el significado
            * que esa key ya tiene persistido.
            */
            return Conflict(
                new
                {
                    message = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
    }
    [HttpPost("{paymentId:guid}/reversal")]
    [ProducesResponseType(
        typeof(PaymentReversalResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaymentReversalResponse>>
        Reverse(
            Guid paymentId,
            [FromBody] PaymentReversalRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            var reversal =
                await _paymentReversalService
                    .ReverseAsync(
                        _currentUserService.TenantId,
                        _currentUserService.AppUserId,
                        paymentId,
                        request,
                        cancellationToken);

            return Ok(
                reversal);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<PaymentResponse>>> GetAllByLoan(
        [FromQuery] Guid loanId,
        CancellationToken cancellationToken)
    {
        if (loanId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "LoanId must be a valid identifier."
            });
        }

        try
        {
            var payments =
                await _paymentService.GetAllByLoanAsync(
                    _currentUserService.TenantId,
                    loanId,
                    cancellationToken);

            return Ok(payments);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var payment =
            await _paymentService.GetByIdAsync(
                _currentUserService.TenantId,
                id,
                cancellationToken);

        if (payment is null)
        {
            return NotFound();
        }

        return Ok(payment);
    }
}