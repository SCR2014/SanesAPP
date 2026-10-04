using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Sanes.Api.IntegrationTests;

public class ForwardedHeadersTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory
        _factory;

    public ForwardedHeadersTests(
        CustomWebApplicationFactory factory)
    {
        _factory =
            factory;
    }

    [Fact]
    public void ForwardedHeaders_AreConfiguredForAzureAppService()
    {
        var options =
            _factory.Services
                .GetRequiredService<
                    IOptions<ForwardedHeadersOptions>>()
                .Value;

        Assert.True(
            options.ForwardedHeaders.HasFlag(
                ForwardedHeaders.XForwardedFor));

        Assert.True(
            options.ForwardedHeaders.HasFlag(
                ForwardedHeaders.XForwardedProto));

        Assert.Contains(
            options.KnownIPNetworks,
            network =>
                network.Equals(
                    new System.Net.IPNetwork(
                        IPAddress.Parse(
                            "::ffff:10.0.0.0"),
                        104)));

        Assert.Contains(
            options.KnownIPNetworks,
            network =>
                network.Equals(
                    new System.Net.IPNetwork(
                        IPAddress.Parse(
                            "::ffff:172.16.0.0"),
                        108)));

        Assert.Contains(
            options.KnownIPNetworks,
            network =>
                network.Equals(
                    new System.Net.IPNetwork(
                        IPAddress.Parse(
                            "::ffff:192.168.0.0"),
                        112)));
    }
}
