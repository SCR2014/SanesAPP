namespace Sanes.Web.Api;

public interface ISanesApiClient
{
    Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default);
}