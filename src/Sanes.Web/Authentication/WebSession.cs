namespace Sanes.Web.Authentication;

public sealed class WebSession
{
    public string AccessToken { get; init; } =
        string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; init; }
}