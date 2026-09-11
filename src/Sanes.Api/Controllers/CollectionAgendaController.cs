using Microsoft.AspNetCore.Mvc;
using Sanes.Application.CollectionAgenda.DTOs;
using Sanes.Application.CollectionAgenda.Services;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/collection-agenda")]
public class CollectionAgendaController : ControllerBase
{
    private readonly ICollectionAgendaService _service;

    public CollectionAgendaController(
        ICollectionAgendaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<CollectionAgendaResponse>> GetDailyAgenda(
        [FromQuery] Guid tenantId,
        [FromQuery] DateOnly date,
        [FromQuery] Guid? appUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _service.GetDailyAgendaAsync(
                    tenantId,
                    date,
                    appUserId,
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
}