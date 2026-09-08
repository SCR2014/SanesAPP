using Microsoft.AspNetCore.Mvc;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Clients.Services;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponse>> Create(
        [FromBody] CreateClientRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = await _clientService.CreateAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = client.Id,
                    tenantId = client.TenantId
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

        var clients = await _clientService.GetAllAsync(
            tenantId,
            cancellationToken);

        return Ok(clients);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientResponse>> GetById(
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

        var client = await _clientService.GetByIdAsync(
            tenantId,
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
        [FromQuery] Guid tenantId,
        [FromBody] UpdateClientRequest request,
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
            var client = await _clientService.UpdateAsync(
                tenantId,
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

        var deleted = await _clientService.DeleteAsync(
            tenantId,
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

        var reactivated = await _clientService.ReactivateAsync(
            tenantId,
            id,
            cancellationToken);

        if (!reactivated)
        {
            return NotFound();
        }

        return NoContent();
    }
}