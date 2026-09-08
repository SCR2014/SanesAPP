using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Tenants.DTOs;
using Sanes.Application.Tenants.Services;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpPost]
    public async Task<ActionResult<TenantDto>> Create(
        CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = tenant.Id },
            tenant);
    }

    [HttpGet]
    public async Task<ActionResult<List<TenantDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var tenants = await _tenantService.GetAllAsync(
            cancellationToken);

        return Ok(tenants);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetByIdAsync(
            id,
            cancellationToken);

        if (tenant is null)
        {
            return NotFound();
        }

        return Ok(tenant);
}

    [HttpPut("{id:guid}")]
public async Task<ActionResult<TenantDto>> Update(
    Guid id,
    UpdateTenantRequest request,
    CancellationToken cancellationToken)
{
    var tenant = await _tenantService.UpdateAsync(
        id,
        request,
        cancellationToken);

    if (tenant is null)
    {
        return NotFound();
    }

    return Ok(tenant);
}

    [HttpDelete("{id:guid}")]
public async Task<IActionResult> Delete(
    Guid id,
    CancellationToken cancellationToken)
{
    var deleted = await _tenantService.DeleteAsync(
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
    var reactivated = await _tenantService.ReactivateAsync(
        id,
        cancellationToken);

    if (!reactivated)
    {
        return NotFound();
    }

    return NoContent();
}
}