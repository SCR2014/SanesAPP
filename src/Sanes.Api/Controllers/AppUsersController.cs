using Microsoft.AspNetCore.Mvc;
using Sanes.Application.AppUsers.DTOs;
using Sanes.Application.AppUsers.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/app-users")]
public class AppUsersController : ControllerBase
{
    private readonly IAppUserService _appUserService;

    public AppUsersController(
        IAppUserService appUserService)
    {
        _appUserService = appUserService;
    }

    [HttpPost]
    public async Task<ActionResult<AppUserResponse>> Create(
        [FromBody] CreateAppUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created =
                await _appUserService.CreateAsync(
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = created.Id,
                    tenantId = created.TenantId
                },
                created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<AppUserResponse>>> GetAll(
        [FromQuery] Guid tenantId,
        [FromQuery] AppUserRole? role,
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
            var appUsers =
                await _appUserService.GetAllAsync(
                    tenantId,
                    role,
                    cancellationToken);

            return Ok(appUsers);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppUserResponse>> GetById(
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

        var appUser =
            await _appUserService.GetByIdAsync(
                id,
                tenantId,
                cancellationToken);

        if (appUser is null)
        {
            return NotFound();
        }

        return Ok(appUser);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AppUserResponse>> Update(
        Guid id,
        [FromQuery] Guid tenantId,
        [FromBody] UpdateAppUserRequest request,
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
            var updated =
                await _appUserService.UpdateAsync(
                    id,
                    tenantId,
                    request,
                    cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
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
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "TenantId must be a valid identifier."
            });
        }

        var deleted =
            await _appUserService.DeleteAsync(
                id,
                tenantId,
                cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPatch("{id:guid}/reactivate")]
    public async Task<ActionResult<AppUserResponse>> Reactivate(
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

        var reactivated =
            await _appUserService.ReactivateAsync(
                id,
                tenantId,
                cancellationToken);

        if (reactivated is null)
        {
            return NotFound();
        }

        return Ok(reactivated);
    }

    [HttpGet("{id:guid}/collection-routes")]
    public async Task<ActionResult<List<AppUserCollectionRouteResponse>>>
        GetCollectionRoutes(
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

        var routes =
            await _appUserService.GetCollectionRoutesAsync(
                id,
                tenantId,
                cancellationToken);

        if (routes is null)
        {
            return NotFound();
        }

        return Ok(routes);
    }

    [HttpPost("{id:guid}/collection-routes/{routeId:guid}")]
    public async Task<IActionResult> AssignCollectionRoute(
        Guid id,
        Guid routeId,
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
            var assigned =
                await _appUserService.AssignCollectionRouteAsync(
                    id,
                    routeId,
                    tenantId,
                    cancellationToken);

            if (!assigned)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpDelete("{id:guid}/collection-routes/{routeId:guid}")]
    public async Task<IActionResult> UnassignCollectionRoute(
        Guid id,
        Guid routeId,
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

        var removed =
            await _appUserService.UnassignCollectionRouteAsync(
                id,
                routeId,
                tenantId,
                cancellationToken);

        if (!removed)
        {
            return NotFound();
        }

        return NoContent();
    }
}