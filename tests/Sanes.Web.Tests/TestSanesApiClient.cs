using Sanes.Web.Api;

namespace Sanes.Web.Tests;

internal sealed class TestSanesApiClient
    : ISanesApiClient
{
    private readonly Queue<HttpResponseMessage>
        _responses = new();

    public List<RecordedRequest> Requests { get; } =
        new();

    public void EnqueueResponse(
        HttpResponseMessage response)
    {
        _responses.Enqueue(response);
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        string? body = null;

        if (request.Content is not null)
        {
            body =
                await request.Content
                    .ReadAsStringAsync(
                        cancellationToken);
        }

        Requests.Add(
            new RecordedRequest(
                request.Method,
                request.RequestUri?.ToString()
                    ?? string.Empty,
                body));

        return _responses.Dequeue();
    }

    public sealed record RecordedRequest(
        HttpMethod Method,
        string Uri,
        string? Body);
}