using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Authentication.Services;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Loans.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/loans/{loanId:guid}/early-settlement")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class EarlySettlementsController : ControllerBase
{
    private readonly IEarlySettlementService
        _earlySettlementService;

    private readonly ICurrentUserService
        _currentUserService;

    public EarlySettlementsController(
        IEarlySettlementService earlySettlementService,
        ICurrentUserService currentUserService)
    {
        _earlySettlementService =
            earlySettlementService;

        _currentUserService =
            currentUserService;
    }

    [HttpPost("quote")]
    [ProducesResponseType(
        typeof(EarlySettlementQuoteResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<EarlySettlementQuoteResponse>>
        Quote(
            Guid loanId,
            [FromBody]
            EarlySettlementQuoteRequest request,
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
                await _earlySettlementService.QuoteAsync(
                    _currentUserService.TenantId,
                    loanId,
                    request,
                    cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
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

    [HttpPost]
    [ProducesResponseType(
        typeof(EarlySettlementResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EarlySettlementResponse>>
        Execute(
            Guid loanId,
            [FromBody]
            EarlySettlementExecuteRequest request,
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
                await _earlySettlementService.ExecuteAsync(
                    _currentUserService.TenantId,
                    _currentUserService.AppUserId,
                    loanId,
                    request,
                    cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
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
    [ProducesResponseType(
        typeof(EarlySettlementResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EarlySettlementResponse>>
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

        var result =
            await _earlySettlementService.GetByLoanAsync(
                _currentUserService.TenantId,
                loanId,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}