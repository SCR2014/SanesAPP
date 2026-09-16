using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Investors.Services;
using Microsoft.AspNetCore.Authorization;
using Sanes.Application.Authentication.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class InvestorsController : ControllerBase
{
    private readonly IInvestorService _investorService;
    private readonly ICurrentUserService _currentUserService;

    public InvestorsController(IInvestorService investorService, ICurrentUserService currentUserService)
    {
        _investorService = investorService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateInvestorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var investor = await _investorService.CreateAsync(
                _currentUserService.TenantId,
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = investor.Id
                },
                investor);
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
    public async Task<ActionResult<List<InvestorResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var investors = await _investorService.GetAllAsync(
            _currentUserService.TenantId,
            cancellationToken);

        return Ok(investors);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvestorResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var investor = await _investorService.GetByIdAsync(
            _currentUserService.TenantId,
            id,
            cancellationToken);

        if (investor is null)
        {
            return NotFound();
        }

        return Ok(investor);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvestorResponse>> Update(
        Guid id,
        [FromBody] UpdateInvestorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var investor = await _investorService.UpdateAsync(
                _currentUserService.TenantId,
                id,
                request,
                cancellationToken);

            if (investor is null)
            {
                return NotFound();
            }

            return Ok(investor);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _investorService.DeleteAsync(
            _currentUserService.TenantId,
            id,
            cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPatch("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var reactivated = await _investorService.ReactivateAsync(
            _currentUserService.TenantId,
            id,
            cancellationToken);

        if (!reactivated)
        {
            return NotFound();
        }

        return NoContent();
    }
}