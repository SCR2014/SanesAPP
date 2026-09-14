using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sanes.Application.Provisioning.DTOs;
using Sanes.Application.Provisioning.Services;
using Sanes.Infrastructure.Provisioning;

namespace Sanes.Api.Controllers;

[ApiController]
[Route("api/provisioning")]
[AllowAnonymous]
public class ProvisioningController : ControllerBase
{
    private readonly IProvisioningService
        _provisioningService;

    private readonly ProvisioningSettings
        _settings;

    public ProvisioningController(
        IProvisioningService provisioningService,
        IOptions<ProvisioningSettings> options)
    {
        _provisioningService = provisioningService;
        _settings = options.Value;
    }

    [HttpPost("tenants")]
    [ProducesResponseType(
        typeof(ProvisionTenantResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProvisionTenantResponse>>
        ProvisionTenant(
            [FromHeader(Name = "X-Provisioning-Key")]
            string? provisioningKey,
            [FromBody]
            ProvisionTenantRequest request,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.Key) ||
            string.IsNullOrWhiteSpace(provisioningKey) ||
            !string.Equals(
                provisioningKey,
                _settings.Key,
                StringComparison.Ordinal))
        {
            return Unauthorized(new
            {
                message = "Invalid provisioning credentials."
            });
        }

        try
        {
            var result =
                await _provisioningService
                    .ProvisionTenantAsync(
                        request,
                        cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
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