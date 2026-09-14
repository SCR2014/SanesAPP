using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Loans.Services;
using Microsoft.AspNetCore.Authorization;
using Sanes.Application.Authentication.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;
    private readonly ICurrentUserService _currentUserService;

    public LoansController(
        ILoanService loanService,
        ICurrentUserService currentUserService)
    {
        _loanService = loanService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<ActionResult<LoanResponse>> Create(
        [FromBody] CreateLoanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var loan = await _loanService.CreateAsync(
                _currentUserService.TenantId,
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = loan.Id
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
        CancellationToken cancellationToken)
    {

        var loans = await _loanService.GetAllAsync(
            _currentUserService.TenantId,
            cancellationToken);

        return Ok(loans);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LoanResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {

        var loan = await _loanService.GetByIdAsync(
            _currentUserService.TenantId,
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
        [FromBody] UpdateLoanRequest request,
        CancellationToken cancellationToken)
    {

        try
        {
            var loan = await _loanService.UpdateAsync(
                _currentUserService.TenantId,
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
        CancellationToken cancellationToken)
    {

        try
        {
            var cancelled = await _loanService.CancelAsync(
                _currentUserService.TenantId,
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
        CancellationToken cancellationToken)
    {

        var summary =
            await _loanService.GetFinancialSummaryAsync(
                _currentUserService.TenantId,
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
        CancellationToken cancellationToken)
    {


        try
        {
            var portfolio =
                await _loanService.GetActivePortfolioAsync(
                    _currentUserService.TenantId,
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
        CancellationToken cancellationToken)
    {

        try
        {
            var summary =
                await _loanService.GetActivePortfolioSummaryAsync(
                    _currentUserService.TenantId,
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
        [FromQuery] bool overdueOnly = false,
        [FromQuery] DateTime? collectionDate = null,
        [FromQuery] DateTime? dueDate = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? investorId = null,
        [FromQuery] Guid? clientId = null,
        [FromQuery] Guid? collectionRouteId = null,
        CancellationToken cancellationToken = default)
    {

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
                    _currentUserService.TenantId,
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
        [FromQuery] bool overdueOnly = false,
        [FromQuery] DateTime? collectionDate = null,
        [FromQuery] DateTime? dueDate = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? investorId = null,
        [FromQuery] Guid? clientId = null,
        [FromQuery] Guid? collectionRouteId = null,
        CancellationToken cancellationToken = default)
    {

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
                    _currentUserService.TenantId,
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