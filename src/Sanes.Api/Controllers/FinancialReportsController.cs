using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Authentication.Services;
using Sanes.Application.FinancialReports.DTOs;
using Sanes.Application.FinancialReports.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class FinancialReportsController : ControllerBase
{
    private readonly IFinancialReportService
        _financialReportService;

    private readonly ICurrentUserService
        _currentUserService;

    public FinancialReportsController(
        IFinancialReportService financialReportService,
        ICurrentUserService currentUserService)
    {
        _financialReportService =
            financialReportService;

        _currentUserService =
            currentUserService;
    }

    /*
     * GET /api/reports/portfolio
     *
     * Filtros opcionales:
     *
     * investorId
     * collectionRouteId
     * overdueOnly
     */
    [HttpGet("portfolio")]
    [ProducesResponseType(
        typeof(FinancialPortfolioReportResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<
        ActionResult<FinancialPortfolioReportResponse>>
        GetPortfolio(
            [FromQuery] Guid? investorId = null,
            [FromQuery] Guid? collectionRouteId = null,
            [FromQuery] bool overdueOnly = false,
            CancellationToken cancellationToken = default)
    {
        if (_currentUserService.TenantId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (
            investorId.HasValue &&
            investorId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "InvestorId must be a valid identifier."
            });
        }

        if (
            collectionRouteId.HasValue &&
            collectionRouteId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "CollectionRouteId must be a valid identifier."
            });
        }

        var result =
            await _financialReportService
                .GetPortfolioAsync(
                    _currentUserService.TenantId,
                    investorId,
                    collectionRouteId,
                    overdueOnly,
                    cancellationToken);

        return Ok(result);
    }

    /*
     * GET /api/reports/delinquency
     *
     * Devuelve:
     *
     * - resumen de la cartera seleccionada
     * - aging de mora
     * - préstamos morosos
     */
    [HttpGet("delinquency")]
    [ProducesResponseType(
        typeof(FinancialDelinquencyReportResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<
        ActionResult<FinancialDelinquencyReportResponse>>
        GetDelinquency(
            [FromQuery] Guid? investorId = null,
            [FromQuery] Guid? collectionRouteId = null,
            CancellationToken cancellationToken = default)
    {
        if (_currentUserService.TenantId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (
            investorId.HasValue &&
            investorId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "InvestorId must be a valid identifier."
            });
        }

        if (
            collectionRouteId.HasValue &&
            collectionRouteId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "CollectionRouteId must be a valid identifier."
            });
        }

        var result =
            await _financialReportService
                .GetDelinquencyAsync(
                    _currentUserService.TenantId,
                    investorId,
                    collectionRouteId,
                    cancellationToken);

        return Ok(result);
    }

    /*
    * GET /api/reports/collections
    *
    * from y to son obligatorios.
    *
    * Filtros opcionales:
    *
    * investorId
    * collectorId
    * collectionRouteId
    */
    [HttpGet("collections")]
    [ProducesResponseType(
        typeof(FinancialCollectionsReportResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<
        ActionResult<FinancialCollectionsReportResponse>>
        GetCollections(
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            [FromQuery] Guid? investorId = null,
            [FromQuery] Guid? collectorId = null,
            [FromQuery] Guid? collectionRouteId = null,
            CancellationToken cancellationToken = default)
    {
        if (_currentUserService.TenantId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (!from.HasValue || !to.HasValue)
        {
            return BadRequest(new
            {
                message =
                    "From and To dates are required."
            });
        }

        if (from.Value > to.Value)
        {
            return BadRequest(new
            {
                message =
                    "From date cannot be greater than To date."
            });
        }

        var inclusiveDays =
            to.Value.DayNumber -
            from.Value.DayNumber +
            1;

        if (inclusiveDays > 366)
        {
            return BadRequest(new
            {
                message =
                    "The report period cannot exceed 366 days."
            });
        }

        if (
            investorId.HasValue &&
            investorId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "InvestorId must be a valid identifier."
            });
        }

        if (
            collectorId.HasValue &&
            collectorId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "CollectorId must be a valid identifier."
            });
        }

        if (
            collectionRouteId.HasValue &&
            collectionRouteId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "CollectionRouteId must be a valid identifier."
            });
        }

        var result =
            await _financialReportService
                .GetCollectionsAsync(
                    _currentUserService.TenantId,
                    from.Value,
                    to.Value,
                    investorId,
                    collectorId,
                    collectionRouteId,
                    cancellationToken);

        return Ok(result);
    }

    /*
    * GET /api/reports/investors/{investorId}
    *
    * Estado financiero completo del inversionista:
    *
    * - historia financiera
    * - cartera actual
    * - morosidad
    * - préstamos activos
    */
    [HttpGet("investors/{investorId:guid}")]
    [ProducesResponseType(
        typeof(FinancialInvestorStatementResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<FinancialInvestorStatementResponse>>
        GetInvestorStatement(
            Guid investorId,
            CancellationToken cancellationToken = default)
    {
        if (_currentUserService.TenantId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (investorId == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "InvestorId must be a valid identifier."
            });
        }

        var result =
            await _financialReportService
                .GetInvestorStatementAsync(
                    _currentUserService.TenantId,
                    investorId,
                    cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}