using Microsoft.AspNetCore.Mvc;
using Sanes.Application.CollectionRouteSchedules.DTOs;
using Sanes.Application.CollectionRouteSchedules.Services;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/collection-route-schedules")]
public class CollectionRouteSchedulesController : ControllerBase
{
    private readonly ICollectionRouteScheduleService _service;

    public CollectionRouteSchedulesController(
        ICollectionRouteScheduleService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<CollectionRouteScheduleResponse>> Create(
        [FromBody] CreateCollectionRouteScheduleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CreateAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = result.Id,
                    tenantId = request.TenantId,
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
        [FromQuery] Guid tenantId,
        [FromQuery] Guid collectionRouteId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetAllAsync(
                tenantId,
                collectionRouteId,
                cancellationToken);

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
        [FromQuery] Guid tenantId,
        [FromQuery] Guid collectionRouteId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(
            id,
            tenantId,
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
        [FromQuery] Guid tenantId,
        [FromQuery] Guid collectionRouteId,
        [FromBody] UpdateCollectionRouteScheduleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.UpdateAsync(
                id,
                tenantId,
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
        [FromQuery] Guid tenantId,
        [FromQuery] Guid collectionRouteId,
        CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(
            id,
            tenantId,
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
        [FromQuery] Guid tenantId,
        [FromQuery] Guid collectionRouteId,
        CancellationToken cancellationToken)
    {
        var result = await _service.ReactivateAsync(
            id,
            tenantId,
            collectionRouteId,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}