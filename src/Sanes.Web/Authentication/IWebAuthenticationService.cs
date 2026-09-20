using Sanes.Application.Authentication.DTOs;

namespace Sanes.Web.Authentication;

public interface IWebAuthenticationService
{
    Task<WebAuthenticationResult> SignInAsync(
        LoginRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);
}