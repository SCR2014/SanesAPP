using Microsoft.AspNetCore.Mvc;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Application.CollectionRoutes.Services;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/collection-routes")]
public class CollectionRoutesController : ControllerBase
{
    private readonly ICollectionRouteService _collectionRouteService;

    public CollectionRoutesController(
        ICollectionRouteService collectionRouteService)
    {
        _collectionRouteService = collectionRouteService;
    }

    [HttpPost]
    public async Task<ActionResult<CollectionRouteResponse>> Create(
        [FromBody] CreateCollectionRouteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var route = await _collectionRouteService.CreateAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = route.Id,
                    tenantId = route.TenantId
                },
                route);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<CollectionRouteResponse>>> GetAll(
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest("tenantId is required.");
        }

        var routes = await _collectionRouteService.GetAllAsync(
            tenantId,
            cancellationToken);

        return Ok(routes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CollectionRouteResponse>> GetById(
        Guid id,
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest("tenantId is required.");
        }

        var route = await _collectionRouteService.GetByIdAsync(
            id,
            tenantId,
            cancellationToken);

        if (route is null)
        {
            return NotFound();
        }

        return Ok(route);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CollectionRouteResponse>> Update(
        Guid id,
        [FromQuery] Guid tenantId,
        [FromBody] UpdateCollectionRouteRequest request,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest("tenantId is required.");
        }

        try
        {
            var route = await _collectionRouteService.UpdateAsync(
                id,
                tenantId,
                request,
                cancellationToken);

            if (route is null)
            {
                return NotFound();
            }

            return Ok(route);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
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
            return BadRequest("tenantId is required.");
        }

        var deleted = await _collectionRouteService.DeleteAsync(
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
    public async Task<ActionResult<CollectionRouteResponse>> Reactivate(
        Guid id,
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest("tenantId is required.");
        }

        var route = await _collectionRouteService.ReactivateAsync(
            id,
            tenantId,
            cancellationToken);

        if (route is null)
        {
            return NotFound();
        }

        return Ok(route);
    }

    [HttpPut("{id:guid}/clients/order")]
    public async Task<IActionResult> ReorderClients(
        Guid id,
        [FromQuery] Guid tenantId,
        [FromBody] ReorderCollectionRouteRequest request,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "TenantId must be a valid identifier."
            });
        }

        if (request is null)
        {
            return BadRequest(new
            {
                message = "Request body is required."
            });
        }

        try
        {
            var reordered =
                await _collectionRouteService.ReorderClientsAsync(
                    id,
                    tenantId,
                    request,
                    cancellationToken);

            if (!reordered)
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

    [HttpPost("{id:guid}/optimize")]
    public async Task<IActionResult> Optimize(
        Guid id,
        [FromQuery] Guid tenantId,
        [FromBody] OptimizeCollectionRouteRequest request,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "TenantId must be a valid identifier."
            });
        }

        if (request is null)
        {
            return BadRequest(new
            {
                message = "Request body is required."
            });
        }

        try
        {
            var optimized =
                await _collectionRouteService.OptimizeClientsAsync(
                    id,
                    tenantId,
                    request,
                    cancellationToken);

            if (!optimized)
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