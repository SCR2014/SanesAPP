using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Clients.Services;
using Microsoft.AspNetCore.Authorization;
using Sanes.Application.Authentication.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly ICurrentUserService _currentUserService;

    public ClientsController(
        IClientService clientService,
        ICurrentUserService currentUserService)
    {
        _clientService = clientService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponse>> Create(
        [FromBody] CreateClientRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = await _clientService.CreateAsync(
                _currentUserService.TenantId,
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = client.Id
                },
                client);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<ClientResponse>>> GetAll(
        [FromQuery] Guid? collectionRouteId,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var clients =
            await _clientService.GetAllAsync(
                _currentUserService.TenantId,
                collectionRouteId,
                cancellationToken,
                includeInactive);

        return Ok(clients);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {

        var client = await _clientService.GetByIdAsync(
            _currentUserService.TenantId,
            id,
            cancellationToken);

        if (client is null)
        {
            return NotFound();
        }

        return Ok(client);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientResponse>> Update(
        Guid id,
        [FromBody] UpdateClientRequest request,
        CancellationToken cancellationToken)
    {

        try
        {
            var client = await _clientService.UpdateAsync(
                _currentUserService.TenantId,
                id,
                request,
                cancellationToken);

            if (client is null)
            {
                return NotFound();
            }

            return Ok(client);
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

        var deleted = await _clientService.DeleteAsync(
            _currentUserService.TenantId,
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

        var reactivated = await _clientService.ReactivateAsync(
            _currentUserService.TenantId,
            id,
            cancellationToken);

        if (!reactivated)
        {
            return NotFound();
        }

        return NoContent();
    }
}