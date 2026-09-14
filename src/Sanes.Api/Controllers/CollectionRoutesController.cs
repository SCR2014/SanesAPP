using Microsoft.AspNetCore.Mvc;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Application.CollectionRoutes.Services;
using Microsoft.AspNetCore.Authorization;
using Sanes.Application.Authentication.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/collection-routes")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class CollectionRoutesController : ControllerBase
{
    private readonly ICollectionRouteService _collectionRouteService;
    private readonly ICurrentUserService _currentUserService;

    public CollectionRoutesController(
        ICollectionRouteService collectionRouteService,
        ICurrentUserService currentUserService)
    {
        _collectionRouteService = collectionRouteService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<ActionResult<CollectionRouteResponse>> Create(
        [FromBody] CreateCollectionRouteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var route = await _collectionRouteService.CreateAsync(
                _currentUserService.TenantId,
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = route.Id
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
        CancellationToken cancellationToken)
    {

        var routes = await _collectionRouteService.GetAllAsync(
            _currentUserService.TenantId,
            cancellationToken);

        return Ok(routes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CollectionRouteResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {

        var route = await _collectionRouteService.GetByIdAsync(
            id,
            _currentUserService.TenantId,
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
        [FromBody] UpdateCollectionRouteRequest request,
        CancellationToken cancellationToken)
    {

        try
        {
            var route = await _collectionRouteService.UpdateAsync(
                id,
                _currentUserService.TenantId,
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
        CancellationToken cancellationToken)
    {

        var deleted = await _collectionRouteService.DeleteAsync(
            id,
            _currentUserService.TenantId,
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
        CancellationToken cancellationToken)
    {

        var route = await _collectionRouteService.ReactivateAsync(
            id,
            _currentUserService.TenantId,
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
        [FromBody] ReorderCollectionRouteRequest request,
        CancellationToken cancellationToken)
    {

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
                    _currentUserService.TenantId,
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
        [FromBody] OptimizeCollectionRouteRequest request,
        CancellationToken cancellationToken)
    {

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
                    _currentUserService.TenantId,
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