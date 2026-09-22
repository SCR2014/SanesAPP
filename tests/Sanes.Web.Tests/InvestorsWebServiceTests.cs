using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.Investors.DTOs;
using Sanes.Web.Investors;

namespace Sanes.Web.Tests;

public class InvestorsWebServiceTests
{
    [Fact]
    public async Task GetAllAsync_WithoutInactive_UsesBaseEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<InvestorResponse>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Investor Activo",
                        IsActive = true
                    }
                }));

        var service =
            new InvestorsWebService(
                apiClient);

        var result =
            await service.GetAllAsync();

        Assert.Single(result);

        Assert.Equal(
            "Investor Activo",
            result[0].Name);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            "api/investors");
    }

    [Fact]
    public async Task GetAllAsync_WithInactive_AddsIncludeInactiveQuery()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<InvestorResponse>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Investor Activo",
                        IsActive = true
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Investor Inactivo",
                        IsActive = false
                    }
                }));

        var service =
            new InvestorsWebService(
                apiClient);

        var result =
            await service.GetAllAsync(
                includeInactive: true);

        Assert.Equal(
            2,
            result.Count);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            "api/investors?includeInactive=true");
    }

    [Fact]
    public async Task CreateAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new InvestorResponse
                {
                    Id = investorId,
                    Name = "Juan Perez",
                    Phone = "8095551234",
                    Email = "juan@sanes.test",
                    Identification = "00112345678",
                    Notes = "Inversionista de prueba",
                    IsActive = true
                },
                HttpStatusCode.Created));

        var service =
            new InvestorsWebService(
                apiClient);

        var request =
            new CreateInvestorRequest
            {
                Name = "Juan Perez",
                Phone = "8095551234",
                Email = "juan@sanes.test",
                Identification = "00112345678",
                Notes = "Inversionista de prueba"
            };

        var result =
            await service.CreateAsync(
                request);

        Assert.Equal(
            investorId,
            result.Id);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Post,
            "api/investors");

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.Equal(
            "Juan Perez",
            root.GetProperty("name")
                .GetString());

        Assert.Equal(
            "8095551234",
            root.GetProperty("phone")
                .GetString());

        Assert.Equal(
            "juan@sanes.test",
            root.GetProperty("email")
                .GetString());

        Assert.Equal(
            "00112345678",
            root.GetProperty("identification")
                .GetString());

        Assert.Equal(
            "Inversionista de prueba",
            root.GetProperty("notes")
                .GetString());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task CreateAsync_WhenApiReturnsBadRequest_ThrowsApiMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "An investor with the same identification already exists for this tenant."
                },
                HttpStatusCode.BadRequest));

        var service =
            new InvestorsWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.CreateAsync(
                        new CreateInvestorRequest
                        {
                            Name =
                                "Investor Duplicado",
                            Identification =
                                "00112345678"
                        }));

        Assert.Equal(
            "An investor with the same identification already exists for this tenant.",
            exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new InvestorResponse
                {
                    Id = investorId,
                    Name = "Investor Actualizado",
                    Phone = "8095559999",
                    IsActive = true
                }));

        var service =
            new InvestorsWebService(
                apiClient);

        var result =
            await service.UpdateAsync(
                investorId,
                new UpdateInvestorRequest
                {
                    Name =
                        "Investor Actualizado",
                    Phone =
                        "8095559999"
                });

        Assert.NotNull(result);

        Assert.Equal(
            "Investor Actualizado",
            result!.Name);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Put,
            $"api/investors/{investorId:D}");

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.Equal(
            "Investor Actualizado",
            root.GetProperty("name")
                .GetString());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new InvestorsWebService(
                apiClient);

        var result =
            await service.UpdateAsync(
                investorId,
                new UpdateInvestorRequest
                {
                    Name = "No Existe"
                });

        Assert.Null(result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Put,
            $"api/investors/{investorId:D}");
    }

    [Fact]
    public async Task DeleteAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            new InvestorsWebService(
                apiClient);

        var result =
            await service.DeleteAsync(
                investorId);

        Assert.True(result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Delete,
            $"api/investors/{investorId:D}");
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new InvestorsWebService(
                apiClient);

        var result =
            await service.DeleteAsync(
                investorId);

        Assert.False(result);
    }

    [Fact]
    public async Task ReactivateAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            new InvestorsWebService(
                apiClient);

        var result =
            await service.ReactivateAsync(
                investorId);

        Assert.True(result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Patch,
            $"api/investors/{investorId:D}/reactivate");
    }

    [Fact]
    public async Task ReactivateAsync_WhenNotFound_ReturnsFalse()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new InvestorsWebService(
                apiClient);

        var result =
            await service.ReactivateAsync(
                investorId);

        Assert.False(result);
    }

    private static void AssertRequest(
        TestSanesApiClient.RecordedRequest request,
        HttpMethod expectedMethod,
        string expectedUri)
    {
        Assert.Equal(
            expectedMethod,
            request.Method);

        Assert.Equal(
            expectedUri,
            request.Uri);

        Assert.False(
            request.Uri.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));
    }

    private static HttpResponseMessage JsonResponse<T>(
        T value,
        HttpStatusCode statusCode =
            HttpStatusCode.OK)
    {
        return new HttpResponseMessage(
            statusCode)
        {
            Content =
                JsonContent.Create(
                    value)
        };
    }
}