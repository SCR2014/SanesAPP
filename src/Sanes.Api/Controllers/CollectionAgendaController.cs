using Microsoft.AspNetCore.Mvc;
using Sanes.Application.CollectionAgenda.DTOs;
using Sanes.Application.CollectionAgenda.Services;
using Microsoft.AspNetCore.Authorization;
using Sanes.Application.Authentication.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/collection-agenda")]
[Authorize(Roles = nameof(AppUserRole.Administrator))]
public class CollectionAgendaController : ControllerBase
{
    private readonly ICollectionAgendaService _service;
    private readonly ICurrentUserService _currentUserService;

    public CollectionAgendaController(
        ICollectionAgendaService service,
        ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<CollectionAgendaResponse>> GetDailyAgenda(
        [FromQuery] DateOnly date,
        [FromQuery] Guid? appUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _service.GetDailyAgendaAsync(
                    _currentUserService.TenantId,
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