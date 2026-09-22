namespace Sanes.Web.Authentication;

public interface IWebSessionStore
{
    Task SetAsync(
        string sessionId,
        WebSession session,
        CancellationToken cancellationToken = default);

    Task<WebSession?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}