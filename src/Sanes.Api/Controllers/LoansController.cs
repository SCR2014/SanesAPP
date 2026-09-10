using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Loans.Services;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpPost]
    public async Task<ActionResult<LoanResponse>> Create(
        [FromBody] CreateLoanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var loan = await _loanService.CreateAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = loan.Id,
                    tenantId = loan.TenantId
                },
                loan);
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
    public async Task<ActionResult<List<LoanResponse>>> GetAll(
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

        var loans = await _loanService.GetAllAsync(
            tenantId,
            cancellationToken);

        return Ok(loans);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LoanResponse>> GetById(
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

        var loan = await _loanService.GetByIdAsync(
            tenantId,
            id,
            cancellationToken);

        if (loan is null)
        {
            return NotFound();
        }

        return Ok(loan);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LoanResponse>> Update(
        Guid id,
        [FromQuery] Guid tenantId,
        [FromBody] UpdateLoanRequest request,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "TenantId must be a valid identifier."
            });
        }

        try
        {
            var loan = await _loanService.UpdateAsync(
                tenantId,
                id,
                request,
                cancellationToken);

            if (loan is null)
            {
                return NotFound();
            }

            return Ok(loan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
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

        try
        {
            var cancelled = await _loanService.CancelAsync(
                tenantId,
                id,
                cancellationToken);

            if (!cancelled)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("{id:guid}/summary")]
    public async Task<ActionResult<LoanFinancialSummaryResponse>> GetFinancialSummary(
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

        var summary =
            await _loanService.GetFinancialSummaryAsync(
                tenantId,
                id,
                cancellationToken);

        if (summary is null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    [HttpGet("portfolio")]
    public async Task<ActionResult<List<ActiveLoanPortfolioItemResponse>>> GetActivePortfolio(
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

        try
        {
            var portfolio =
                await _loanService.GetActivePortfolioAsync(
                    tenantId,
                    cancellationToken);

            return Ok(portfolio);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("portfolio/summary")]
    public async Task<ActionResult<ActivePortfolioSummaryResponse>> GetActivePortfolioSummary(
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

        try
        {
            var summary =
                await _loanService.GetActivePortfolioSummaryAsync(
                    tenantId,
                    cancellationToken);

            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

   [HttpGet("collections")]
    public async Task<ActionResult<List<CollectionLoanItemResponse>>> GetCollectionPortfolio(
        [FromQuery] Guid tenantId,
        [FromQuery] bool overdueOnly = false,
        [FromQuery] DateTime? collectionDate = null,
        [FromQuery] DateTime? dueDate = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? investorId = null,
        [FromQuery] Guid? clientId = null,
        [FromQuery] Guid? collectionRouteId = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "TenantId must be a valid identifier."
            });
        }

        if (collectionDate.HasValue && dueDate.HasValue)
        {
            return BadRequest(new
            {
                message = "collectionDate and dueDate cannot be used at the same time."
            });
        }

        if (investorId.HasValue && investorId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "InvestorId must be a valid identifier."
            });
        }

        if (clientId.HasValue && clientId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "ClientId must be a valid identifier."
            });
        }

        if (collectionRouteId.HasValue &&
            collectionRouteId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "CollectionRouteId must be a valid identifier."
            });
        }

        try
        {
            var portfolio =
                await _loanService.GetCollectionPortfolioAsync(
                    tenantId,
                    overdueOnly,
                    collectionDate,
                    dueDate,
                    search,
                    investorId,
                    clientId,
                    collectionRouteId,
                    cancellationToken);

            return Ok(portfolio);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("collections/summary")]
    public async Task<ActionResult<CollectionPortfolioSummaryResponse>> GetCollectionPortfolioSummary(
        [FromQuery] Guid tenantId,
        [FromQuery] bool overdueOnly = false,
        [FromQuery] DateTime? collectionDate = null,
        [FromQuery] DateTime? dueDate = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? investorId = null,
        [FromQuery] Guid? clientId = null,
        [FromQuery] Guid? collectionRouteId = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "TenantId must be a valid identifier."
            });
        }

        if (collectionDate.HasValue && dueDate.HasValue)
        {
            return BadRequest(new
            {
                message = "collectionDate and dueDate cannot be used at the same time."
            });
        }

        if (investorId.HasValue && investorId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "InvestorId must be a valid identifier."
            });
        }

        if (clientId.HasValue && clientId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "ClientId must be a valid identifier."
            });
        }

        if (collectionRouteId.HasValue &&
            collectionRouteId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "CollectionRouteId must be a valid identifier."
            });
        }

        try
        {
            var summary =
                await _loanService.GetCollectionPortfolioSummaryAsync(
                    tenantId,
                    overdueOnly,
                    collectionDate,
                    dueDate,
                    search,
                    investorId,
                    clientId,
                    collectionRouteId,
                    cancellationToken);

            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}