using Sanes.Domain.Enums;

namespace Sanes.Web.Authentication;

public static class WebRoles
{
    public const string Administrator =
        nameof(AppUserRole.Administrator);

    public const string Collector =
        nameof(AppUserRole.Collector);
}