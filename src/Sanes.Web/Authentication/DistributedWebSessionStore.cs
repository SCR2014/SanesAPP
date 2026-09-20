using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Sanes.Web.Authentication;

public sealed class DistributedWebSessionStore
    : IWebSessionStore
{
    private const string KeyPrefix =
        "sanes:web-session:";

    private readonly IDistributedCache _cache;

    public DistributedWebSessionStore(
        IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task SetAsync(
        string sessionId,
        WebSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            sessionId);

        ArgumentNullException.ThrowIfNull(
            session);

        var options =
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration =
                    session.ExpiresAtUtc
            };

        var json =
            JsonSerializer.Serialize(session);

        await _cache.SetStringAsync(
            BuildKey(sessionId),
            json,
            options,
            cancellationToken);
    }

    public async Task<WebSession?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        var json =
            await _cache.GetStringAsync(
                BuildKey(sessionId),
                cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var session =
            JsonSerializer.Deserialize<WebSession>(
                json);

        if (session is null)
        {
            return null;
        }

        if (session.ExpiresAtUtc <=
            DateTimeOffset.UtcNow)
        {
            await RemoveAsync(
                sessionId,
                cancellationToken);

            return null;
        }

        return session;
    }

    public Task RemoveAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.CompletedTask;
        }

        return _cache.RemoveAsync(
            BuildKey(sessionId),
            cancellationToken);
    }

    private static string BuildKey(
        string sessionId)
    {
        return $"{KeyPrefix}{sessionId}";
    }
}