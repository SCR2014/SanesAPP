using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Authentication.Services;
using Sanes.Application.LateFees.DTOs;
using Sanes.Application.LateFees.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/late-fees")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class LateFeesController : ControllerBase
{
    private readonly ILateFeeAdministrationService
        _lateFeeAdministrationService;

    private readonly ICurrentUserService
        _currentUserService;

    public LateFeesController(
        ILateFeeAdministrationService lateFeeAdministrationService,
        ICurrentUserService currentUserService)
    {
        _lateFeeAdministrationService =
            lateFeeAdministrationService;

        _currentUserService =
            currentUserService;
    }

    [HttpGet("loans/{loanId:guid}")]
    [ProducesResponseType(
        typeof(LateFeeLoanResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LateFeeLoanResponse>>
        GetByLoan(
            Guid loanId,
            CancellationToken cancellationToken)
    {
        if (loanId == Guid.Empty)
        {
            return BadRequest(
                new
                {
                    message =
                        "LoanId must be a valid identifier."
                });
        }

        try
        {
            var result =
                await _lateFeeAdministrationService
                    .GetByLoanAsync(
                        _currentUserService.TenantId,
                        loanId,
                        cancellationToken);

            if (result is null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Loan not found."
                    });
            }

            return Ok(result);
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

    [HttpPost("charges/{chargeId:guid}/adjustments")]
    [ProducesResponseType(
        typeof(LateFeeChargeResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LateFeeChargeResponse>>
        AddAdjustment(
            Guid chargeId,
            [FromBody] LateFeeAdjustmentRequest request,
            CancellationToken cancellationToken)
    {
        if (chargeId == Guid.Empty)
        {
            return BadRequest(
                new
                {
                    message =
                        "Late fee charge id must be a valid identifier."
                });
        }

        try
        {
            var result =
                await _lateFeeAdministrationService
                    .AddAdjustmentAsync(
                        _currentUserService.TenantId,
                        _currentUserService.AppUserId,
                        chargeId,
                        request,
                        cancellationToken);

            if (result is null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Late fee charge not found."
                    });
            }

            return StatusCode(
                StatusCodes.Status201Created,
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
    }
}