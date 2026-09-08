using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Payments.DTOs;
using Sanes.Application.Payments.Services;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(
        IPaymentService paymentService)
    {
        _paymentService = paymentService;
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
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = payment.Id,
                    tenantId = payment.TenantId
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
        [FromQuery] Guid tenantId,
        [FromQuery] Guid loanId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "TenantId must be a valid identifier."
            });
        }

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
                    tenantId,
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
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "TenantId must be a valid identifier."
            });
        }

        var payment =
            await _paymentService.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        if (payment is null)
        {
            return NotFound();
        }

        return Ok(payment);
    }
}