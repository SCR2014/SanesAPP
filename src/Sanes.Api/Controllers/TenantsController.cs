using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanes.Api.Authentication;
using Sanes.Application.Authentication.Services;
using Sanes.Application.Tenants.DTOs;
using Sanes.Application.Tenants.Services;
using Sanes.Domain.Enums;
namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public TenantsController(
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<TenantDto>> GetCurrent(
        CancellationToken cancellationToken)
    {
        var tenant =
            await _tenantService.GetByIdAsync(
                _currentUserService.TenantId,
                cancellationToken);

        if (tenant is null)
        {
            return NotFound();
        }

        return Ok(tenant);
    }

    [HttpPut("me")]
    public async Task<ActionResult<TenantDto>> UpdateCurrent(
        [FromBody] UpdateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var tenant =
            await _tenantService.UpdateAsync(
                _currentUserService.TenantId,
                request,
                cancellationToken);

        if (tenant is null)
        {
            return NotFound();
        }

        return Ok(tenant);
    }
}