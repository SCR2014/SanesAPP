using Microsoft.AspNetCore.Mvc;
using Sanes.Application.FieldCollections.DTOs;
using Sanes.Application.FieldCollections.Services;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/field-collections")]
public class FieldCollectionsController : ControllerBase
{
    private readonly IFieldCollectionService _fieldCollectionService;

    public FieldCollectionsController(
        IFieldCollectionService fieldCollectionService)
    {
        _fieldCollectionService = fieldCollectionService;
    }

    [HttpGet("daily")]
    [ProducesResponseType(
        typeof(FieldCollectionDailyResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FieldCollectionDailyResponse>> GetDaily(
        [FromQuery] Guid tenantId,
        [FromQuery] Guid appUserId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _fieldCollectionService.GetDailyAsync(
                    tenantId,
                    appUserId,
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