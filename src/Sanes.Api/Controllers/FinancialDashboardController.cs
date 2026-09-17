using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Authentication.Services;
using Sanes.Application.Dashboard.DTOs;
using Sanes.Application.Dashboard.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class FinancialDashboardController
    : ControllerBase
{
    private readonly IFinancialDashboardService
        _financialDashboardService;

    private readonly ICurrentUserService
        _currentUserService;

    public FinancialDashboardController(
        IFinancialDashboardService financialDashboardService,
        ICurrentUserService currentUserService)
    {
        _financialDashboardService =
            financialDashboardService;

        _currentUserService =
            currentUserService;
    }

    [HttpGet("summary")]
    [ProducesResponseType(
        typeof(FinancialDashboardSummaryResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FinancialDashboardSummaryResponse>>
        GetSummaryAsync(
            CancellationToken cancellationToken)
    {
        var tenantId =
            _currentUserService.TenantId;

        if (tenantId == Guid.Empty)
        {
            return Unauthorized();
        }

        var response =
            await _financialDashboardService
                .GetSummaryAsync(
                    tenantId,
                    cancellationToken);

        return Ok(
            response);
    }

    [HttpGet("cash-flow")]
    [ProducesResponseType(
        typeof(FinancialDashboardCashFlowResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FinancialDashboardCashFlowResponse>>
        GetCashFlowAsync(
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to,
            CancellationToken cancellationToken)
    {
        var tenantId =
            _currentUserService.TenantId;

        if (tenantId == Guid.Empty)
        {
            return Unauthorized();
        }

        try
        {
            var response =
                await _financialDashboardService
                    .GetCashFlowAsync(
                        tenantId,
                        from,
                        to,
                        cancellationToken);

            return Ok(
                response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                new
                {
                    message =
                        ex.Message
                });
        }
    }

    [HttpGet("investors")]
    [ProducesResponseType(
        typeof(List<FinancialDashboardInvestorBreakdownResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<
        List<FinancialDashboardInvestorBreakdownResponse>>>
        GetInvestorsAsync(
            CancellationToken cancellationToken)
    {
        var tenantId =
            _currentUserService.TenantId;

        if (tenantId == Guid.Empty)
        {
            return Unauthorized();
        }

        var response =
            await _financialDashboardService
                .GetInvestorsAsync(
                    tenantId,
                    cancellationToken);

        return Ok(
            response);
    }

    [HttpGet("routes")]
    [ProducesResponseType(
        typeof(List<FinancialDashboardRouteBreakdownResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<
        List<FinancialDashboardRouteBreakdownResponse>>>
        GetRoutesAsync(
            CancellationToken cancellationToken)
    {
        var tenantId =
            _currentUserService.TenantId;

        if (tenantId == Guid.Empty)
        {
            return Unauthorized();
        }

        var response =
            await _financialDashboardService
                .GetRoutesAsync(
                    tenantId,
                    cancellationToken);

        return Ok(
            response);
    }
}