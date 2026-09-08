using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Investors.Services;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvestorsController : ControllerBase
{
    private readonly IInvestorService _investorService;

    public InvestorsController(IInvestorService investorService)
    {
        _investorService = investorService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateInvestorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var investor = await _investorService.CreateAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = investor.Id,
                    tenantId = investor.TenantId
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
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken)
    {
        var investors = await _investorService.GetAllAsync(
            tenantId,
            cancellationToken);

        return Ok(investors);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvestorResponse>> GetById(
        Guid id,
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken)
    {
        var investor = await _investorService.GetByIdAsync(
            tenantId,
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
        [FromQuery] Guid tenantId,
        [FromBody] UpdateInvestorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var investor = await _investorService.UpdateAsync(
                tenantId,
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
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken)
    {
        var deleted = await _investorService.DeleteAsync(
            tenantId,
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
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken)
    {
        var reactivated = await _investorService.ReactivateAsync(
            tenantId,
            id,
            cancellationToken);

        if (!reactivated)
        {
            return NotFound();
        }

        return NoContent();
    }
}