using System.Net.Http.Json;

namespace Sanes.Api.IntegrationTests.Helpers;

public static class IdempotentHttpClientExtensions
{
    public static Task<HttpResponseMessage>
        PostAsJsonWithIdempotencyAsync<TValue>(
            this HttpClient client,
            string requestUri,
            TValue value,
            CancellationToken cancellationToken = default)
    {
        return client.PostAsJsonWithIdempotencyAsync(
            requestUri,
            value,
            Guid.NewGuid(),
            cancellationToken);
    }

    public static async Task<HttpResponseMessage>
        PostAsJsonWithIdempotencyAsync<TValue>(
            this HttpClient client,
            string requestUri,
            TValue value,
            Guid idempotencyKey,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            client);

        if (string.IsNullOrWhiteSpace(
                requestUri))
        {
            throw new ArgumentException(
                "Request URI is required.",
                nameof(requestUri));
        }

        if (idempotencyKey == Guid.Empty)
        {
            throw new ArgumentException(
                "Idempotency key cannot be empty.",
                nameof(idempotencyKey));
        }

        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                requestUri)
            {
                Content =
                    JsonContent.Create(
                        value)
            };

        message.Headers.TryAddWithoutValidation(
            "Idempotency-Key",
            idempotencyKey.ToString("D"));

        return await client.SendAsync(
            message,
            cancellationToken);
    }
}