using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Authentication.Services;
using Sanes.Application.FinancialReports.DTOs;
using Sanes.Application.FinancialReports.Services;
using Sanes.Application.FinancialReports.Exports;
using Sanes.Application.Tenants.DTOs;
using Sanes.Application.Tenants.Services;
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

    private readonly IFinancialReportExportService
        _financialReportExportService;

    private readonly ITenantService
        _tenantService;

    public FinancialReportsController(
        IFinancialReportService financialReportService,
        IFinancialReportExportService financialReportExportService,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _financialReportService =
            financialReportService;

        _financialReportExportService =
            financialReportExportService;

        _tenantService =
            tenantService;

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

    [HttpGet("portfolio/export/xlsx")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult>
        ExportPortfolioToExcel(
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

        var tenant =
            await GetCurrentTenantAsync(
                cancellationToken);

        if (tenant is null)
        {
            return Unauthorized();
        }

        var report =
            await _financialReportService
                .GetPortfolioAsync(
                    _currentUserService.TenantId,
                    investorId,
                    collectionRouteId,
                    overdueOnly,
                    cancellationToken);

        var context =
            BuildExportContext(
                tenant);

        if (investorId.HasValue)
        {
            var investorName =
                report.Items
                    .FirstOrDefault()
                    ?.InvestorName;

            context.Filters.Add(
                BuildEntityFilter(
                    "Inversionista",
                    investorName,
                    investorId.Value));
        }

        if (collectionRouteId.HasValue)
        {
            var routeName =
                report.Items
                    .FirstOrDefault()
                    ?.CollectionRouteName;

            context.Filters.Add(
                BuildEntityFilter(
                    "Ruta",
                    routeName,
                    collectionRouteId.Value));
        }

        if (overdueOnly)
        {
            context.Filters.Add(
                "Solo cartera vencida: Sí");
        }

        var export =
            _financialReportExportService
                .ExportPortfolioToExcel(
                    report,
                    context);

        return File(
            export.Content,
            export.ContentType,
            export.FileName);
    }

    [HttpGet("delinquency/export/xlsx")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult>
        ExportDelinquencyToExcel(
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

        var tenant =
            await GetCurrentTenantAsync(
                cancellationToken);

        if (tenant is null)
        {
            return Unauthorized();
        }

        var report =
            await _financialReportService
                .GetDelinquencyAsync(
                    _currentUserService.TenantId,
                    investorId,
                    collectionRouteId,
                    cancellationToken);

        var context =
            BuildExportContext(
                tenant);

        if (investorId.HasValue)
        {
            var investorName =
                report.Items
                    .FirstOrDefault()
                    ?.InvestorName;

            context.Filters.Add(
                BuildEntityFilter(
                    "Inversionista",
                    investorName,
                    investorId.Value));
        }

        if (collectionRouteId.HasValue)
        {
            var routeName =
                report.Items
                    .FirstOrDefault()
                    ?.CollectionRouteName;

            context.Filters.Add(
                BuildEntityFilter(
                    "Ruta",
                    routeName,
                    collectionRouteId.Value));
        }

        var export =
            _financialReportExportService
                .ExportDelinquencyToExcel(
                    report,
                    context);

        return File(
            export.Content,
            export.ContentType,
            export.FileName);
    }

    [HttpGet("collections/export/xlsx")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult>
        ExportCollectionsToExcel(
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

        var tenant =
            await GetCurrentTenantAsync(
                cancellationToken);

        if (tenant is null)
        {
            return Unauthorized();
        }

        var report =
            await _financialReportService
                .GetCollectionsAsync(
                    _currentUserService.TenantId,
                    from.Value,
                    to.Value,
                    investorId,
                    collectorId,
                    collectionRouteId,
                    cancellationToken);

        var context =
            BuildExportContext(
                tenant);

        if (investorId.HasValue)
        {
            var investorName =
                report.Items
                    .FirstOrDefault()
                    ?.InvestorName;

            context.Filters.Add(
                BuildEntityFilter(
                    "Inversionista",
                    investorName,
                    investorId.Value));
        }

        if (collectorId.HasValue)
        {
            var collectorName =
                report.Items
                    .FirstOrDefault()
                    ?.CollectorName;

            context.Filters.Add(
                BuildEntityFilter(
                    "Cobrador",
                    collectorName,
                    collectorId.Value));
        }

        if (collectionRouteId.HasValue)
        {
            var routeName =
                report.Items
                    .FirstOrDefault()
                    ?.CollectionRouteName;

            context.Filters.Add(
                BuildEntityFilter(
                    "Ruta histórica",
                    routeName,
                    collectionRouteId.Value));
        }

        var export =
            _financialReportExportService
                .ExportCollectionsToExcel(
                    report,
                    context);

        return File(
            export.Content,
            export.ContentType,
            export.FileName);
    }

    [HttpGet(
        "investors/{investorId:guid}/export/xlsx")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult>
        ExportInvestorStatementToExcel(
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

        var tenant =
            await GetCurrentTenantAsync(
                cancellationToken);

        if (tenant is null)
        {
            return Unauthorized();
        }

        var report =
            await _financialReportService
                .GetInvestorStatementAsync(
                    _currentUserService.TenantId,
                    investorId,
                    cancellationToken);

        if (report is null)
        {
            return NotFound();
        }

        var context =
            BuildExportContext(
                tenant);

        context.Filters.Add(
            $"Inversionista: {report.InvestorName}");

        var export =
            _financialReportExportService
                .ExportInvestorStatementToExcel(
                    report,
                    context);

        return File(
            export.Content,
            export.ContentType,
            export.FileName);
    }

    private async Task<TenantDto?>
        GetCurrentTenantAsync(
            CancellationToken cancellationToken)
    {
        return await _tenantService
            .GetByIdAsync(
                _currentUserService.TenantId,
                cancellationToken);
    }

    private static FinancialReportExportContext
        BuildExportContext(
            TenantDto tenant)
    {
        return new FinancialReportExportContext
        {
            TenantName =
                tenant.Name,

            CurrencyCode =
                tenant.CurrencyCode,

            CurrencySymbol =
                tenant.CurrencySymbol,

            GeneratedAtUtc =
                DateTime.UtcNow
        };
    }

    private static string BuildEntityFilter(
        string label,
        string? name,
        Guid id)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return $"{label}: {name}";
        }

        /*
        * Un filtro válido puede producir cero filas.
        * En ese escenario seguimos documentando el
        * identificador solicitado en el Excel.
        */
        return $"{label}: {id}";
    }
}