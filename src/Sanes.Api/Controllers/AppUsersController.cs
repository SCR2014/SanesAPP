using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanes.Api.Authentication;
using Sanes.Application.AppUsers.DTOs;
using Sanes.Application.AppUsers.Services;
using Sanes.Application.Authentication.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/app-users")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class AppUsersController : ControllerBase
{
    private readonly IAppUserService _appUserService;
    private readonly ICurrentUserService _currentUserService;

    public AppUsersController(
        IAppUserService appUserService,
        ICurrentUserService currentUserService)
    {
        _appUserService = appUserService;
        _currentUserService = currentUserService;
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
                    _currentUserService.TenantId,
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = created.Id
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
        [FromQuery] AppUserRole? role,
        CancellationToken cancellationToken)
    {
        try
        {
            var appUsers =
                await _appUserService.GetAllAsync(
                    _currentUserService.TenantId,
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
        CancellationToken cancellationToken)
    {
        var appUser =
            await _appUserService.GetByIdAsync(
                id,
                _currentUserService.TenantId,
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
        [FromBody] UpdateAppUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated =
                await _appUserService.UpdateAsync(
                    id,
                    _currentUserService.TenantId,
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
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted =
                await _appUserService.DeleteAsync(
                    id,
                    _currentUserService.TenantId,
                    cancellationToken);

            if (!deleted)
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

    [HttpPatch("{id:guid}/reactivate")]
    public async Task<ActionResult<AppUserResponse>> Reactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var reactivated =
            await _appUserService.ReactivateAsync(
                id,
                _currentUserService.TenantId,
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
            CancellationToken cancellationToken)
    {
        var routes =
            await _appUserService.GetCollectionRoutesAsync(
                id,
                _currentUserService.TenantId,
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
        CancellationToken cancellationToken)
    {
        try
        {
            var assigned =
                await _appUserService.AssignCollectionRouteAsync(
                    id,
                    routeId,
                    _currentUserService.TenantId,
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
        CancellationToken cancellationToken)
    {
        var removed =
            await _appUserService.UnassignCollectionRouteAsync(
                id,
                routeId,
                _currentUserService.TenantId,
                cancellationToken);

        if (!removed)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPatch("{id:guid}/password")]
    public async Task<IActionResult> SetPassword(
        Guid id,
        [FromBody] SetAppUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated =
                await _appUserService.SetPasswordAsync(
                    id,
                    _currentUserService.TenantId,
                    request.Password,
                    cancellationToken);

            if (!updated)
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
}