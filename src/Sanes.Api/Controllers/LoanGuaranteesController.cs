using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Authentication.Services;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Loans.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/loans/{loanId:guid}/guarantee")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class LoanGuaranteesController
    : ControllerBase
{
    private readonly ILoanGuaranteeService
        _loanGuaranteeService;

    private readonly ICurrentUserService
        _currentUserService;

    public LoanGuaranteesController(
        ILoanGuaranteeService loanGuaranteeService,
        ICurrentUserService currentUserService)
    {
        _loanGuaranteeService =
            loanGuaranteeService;

        _currentUserService =
            currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(LoanGuaranteeResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanGuaranteeResponse>>
        GetByLoan(
            Guid loanId,
            CancellationToken cancellationToken)
    {
        var result =
            await _loanGuaranteeService.GetByLoanAsync(
                _currentUserService.TenantId,
                loanId,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(
            result);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(LoanGuaranteeResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanGuaranteeResponse>>
        Create(
            Guid loanId,
            [FromBody]
            CreateLoanGuaranteeRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _loanGuaranteeService.CreateAsync(
                    _currentUserService.TenantId,
                    loanId,
                    request,
                    cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            return CreatedAtAction(
                nameof(GetByLoan),
                new
                {
                    loanId
                },
                result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                new
                {
                    message =
                        ex.Message
                });
        }
    }

    [HttpPut]
    [ProducesResponseType(
        typeof(LoanGuaranteeResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanGuaranteeResponse>>
        Update(
            Guid loanId,
            [FromBody]
            UpdateLoanGuaranteeRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _loanGuaranteeService.UpdateAsync(
                    _currentUserService.TenantId,
                    loanId,
                    request,
                    cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(
                result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                new
                {
                    message =
                        ex.Message
                });
        }
    }
}