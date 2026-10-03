using Microsoft.AspNetCore.Mvc;
using Sanes.Application.FieldCollections.DTOs;
using Sanes.Application.FieldCollections.Services;
using Microsoft.AspNetCore.Authorization;
using Sanes.Application.Authentication.Services;
using Sanes.Domain.Enums;
using Sanes.Application.Payments.Exceptions;
using Sanes.Application.Payments.Models;
using Sanes.Application.Payments.Services;

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
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FieldCollectionPaymentResponse>>
        CreatePayment(
            [FromBody] CreateFieldCollectionPaymentRequest request,
            [FromHeader(Name = "Idempotency-Key")]
            string? idempotencyKey,
            CancellationToken cancellationToken)
    {
        /*
        * Cada cobro iniciado por el dispositivo del cobrador
        * debe conservar la misma key mientras se reintente
        * esa misma operación lógica.
        */
        if (string.IsNullOrWhiteSpace(
                idempotencyKey))
        {
            return BadRequest(
                new
                {
                    message =
                        "Idempotency-Key header is required."
                });
        }

        if (!Guid.TryParse(
                idempotencyKey.Trim(),
                out var parsedIdempotencyKey) ||
            parsedIdempotencyKey == Guid.Empty)
        {
            return BadRequest(
                new
                {
                    message =
                        "Idempotency-Key must be a valid non-empty GUID."
                });
        }

        /*
        * Para Field Collections el fingerprint contiene:
        *
        * - cobrador autenticado
        * - ruta
        * - préstamo
        * - monto
        * - tipo
        * - notas
        *
        * PaymentDate se excluye porque la genera el servidor.
        */
        var requestHash =
            PaymentIdempotencyFingerprint
                .CreateFieldCollection(
                    _currentUserService.AppUserId,
                    request);

        var idempotencyContext =
            new PaymentIdempotencyContext(
                parsedIdempotencyKey,
                requestHash);

        try
        {
            var result =
                await _fieldCollectionService
                    .CreatePaymentAsync(
                        _currentUserService.TenantId,
                        _currentUserService.AppUserId,
                        request,
                        idempotencyContext,
                        cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }
        catch (PaymentIdempotencyConflictException ex)
        {
            return Conflict(
                new
                {
                    message = ex.Message
                });
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