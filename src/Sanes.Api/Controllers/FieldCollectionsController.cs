using Microsoft.AspNetCore.Mvc;
using Sanes.Application.FieldCollections.DTOs;
using Sanes.Application.FieldCollections.Services;
using Microsoft.AspNetCore.Authorization;
using Sanes.Application.Authentication.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/field-collections")]
[Authorize(Roles = nameof(AppUserRole.Collector))]
public class FieldCollectionsController : ControllerBase
{
    private readonly IFieldCollectionService _fieldCollectionService;
    private readonly ICurrentUserService _currentUserService;

    public FieldCollectionsController(
        IFieldCollectionService fieldCollectionService,
        ICurrentUserService currentUserService)
    {
        _fieldCollectionService = fieldCollectionService;
        _currentUserService = currentUserService;
    }

    [HttpGet("daily")]
    [ProducesResponseType(
        typeof(FieldCollectionDailyResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FieldCollectionDailyResponse>> GetDaily(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _fieldCollectionService.GetDailyAsync(
                    _currentUserService.TenantId,
                    _currentUserService.AppUserId,
                    date,
                    cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
    }

    [HttpPost("payments")]
    [ProducesResponseType(
        typeof(FieldCollectionPaymentResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FieldCollectionPaymentResponse>> CreatePayment(
        [FromBody] CreateFieldCollectionPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _fieldCollectionService.CreatePaymentAsync(
                    _currentUserService.TenantId,
                    _currentUserService.AppUserId,
                    request,
                    cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
    }
}