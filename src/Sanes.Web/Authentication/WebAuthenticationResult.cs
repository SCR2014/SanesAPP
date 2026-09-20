using Sanes.Domain.Enums;

namespace Sanes.Web.Authentication;

public sealed class WebAuthenticationResult
{
    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public AppUserRole? Role { get; init; }

    public static WebAuthenticationResult Success(
        AppUserRole role)
    {
        return new WebAuthenticationResult
        {
            Succeeded = true,
            Role = role
        };
    }

    public static WebAuthenticationResult Failure(
        string errorMessage)
    {
        return new WebAuthenticationResult
        {
            Succeeded = false,
            ErrorMessage = errorMessage
        };
    }
}