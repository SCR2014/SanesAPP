using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Authentication.Services;
using Sanes.Application.Payments.DTOs;
using Sanes.Application.Payments.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserService _currentUserService;

    public PaymentsController(
        IPaymentService paymentService,
        ICurrentUserService currentUserService)
    {
        _paymentService = paymentService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> Create(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var payment =
                await _paymentService.CreateAsync(
                    _currentUserService.TenantId,
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = payment.Id
                },
                payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
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