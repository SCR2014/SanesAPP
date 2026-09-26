using Microsoft.AspNetCore.Mvc;
using Sanes.Application.CollectionRouteSchedules.DTOs;
using Sanes.Application.CollectionRouteSchedules.Services;
using Microsoft.AspNetCore.Authorization;
using Sanes.Application.Authentication.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/collection-route-schedules")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class CollectionRouteSchedulesController : ControllerBase
{
    private readonly ICollectionRouteScheduleService _service;
    private readonly ICurrentUserService _currentUserService;

    public CollectionRouteSchedulesController(
        ICollectionRouteScheduleService service,
        ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<ActionResult<CollectionRouteScheduleResponse>> Create(
        [FromBody] CreateCollectionRouteScheduleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CreateAsync(
                _currentUserService.TenantId,
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = result.Id,
                    collectionRouteId = request.CollectionRouteId
                },
                result);
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
    public async Task<ActionResult<List<CollectionRouteScheduleResponse>>> GetAll(
        [FromQuery] Guid collectionRouteId,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _service.GetAllAsync(
                    _currentUserService.TenantId,
                    collectionRouteId,
                    cancellationToken,
                    includeInactive);

            return Ok(result);
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
    public async Task<ActionResult<CollectionRouteScheduleResponse>> GetById(
        Guid id,
        [FromQuery] Guid collectionRouteId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(
            id,
            _currentUserService.TenantId,
            collectionRouteId,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CollectionRouteScheduleResponse>> Update(
        Guid id,
        [FromQuery] Guid collectionRouteId,
        [FromBody] UpdateCollectionRouteScheduleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.UpdateAsync(
                id,
                _currentUserService.TenantId,
                collectionRouteId,
                request,
                cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
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
        [FromQuery] Guid collectionRouteId,
        CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(
            id,
            _currentUserService.TenantId,
            collectionRouteId,
            cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPatch("{id:guid}/reactivate")]
    public async Task<ActionResult<CollectionRouteScheduleResponse>> Reactivate(
        Guid id,
        [FromQuery] Guid collectionRouteId,
        CancellationToken cancellationToken)
    {
        var result = await _service.ReactivateAsync(
            id,
            _currentUserService.TenantId,
            collectionRouteId,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}