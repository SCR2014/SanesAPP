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

    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(
            new RecordedRequest(
                request.Method,
                request.RequestUri?.ToString()
                    ?? string.Empty));

        return Task.FromResult(
            _responses.Dequeue());
    }

    public sealed record RecordedRequest(
        HttpMethod Method,
        string Uri);
}