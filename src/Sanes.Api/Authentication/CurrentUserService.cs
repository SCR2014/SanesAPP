using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Sanes.Application.Authentication.Services;
using Sanes.Domain.Enums;

namespace Sanes.Api.Authentication;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User =>
        _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public Guid AppUserId =>
        GetGuidClaim(
            ClaimTypes.NameIdentifier,
            JwtRegisteredClaimNames.Sub);

    public Guid TenantId =>
        GetGuidClaim("tenant_id");

    public string? Username =>
        User?.FindFirst("username")?.Value;

    public AppUserRole Role
    {
        get
        {
            var value =
                User?.FindFirst(ClaimTypes.Role)?.Value;

            if (!Enum.TryParse<AppUserRole>(
                    value,
                    ignoreCase: true,
                    out var role))
            {
                throw new InvalidOperationException(
                    "Authenticated user role is invalid.");
            }

            return role;
        }
    }

    private Guid GetGuidClaim(
        params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value =
                User?.FindFirst(claimType)?.Value;

            if (Guid.TryParse(value, out var id))
            {
                return id;
            }
        }

        throw new InvalidOperationException(
            "Required authentication claim is missing.");
    }
}