using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.Tenants.DTOs;
using Sanes.Domain.Enums;
using Sanes.Web.TenantSettings;

namespace Sanes.Web.Tests;

public class TenantSettingsWebServiceTests
{
    [Fact]
    public async Task GetCurrentAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var tenant =
            CreateTenantDto();

        apiClient.EnqueueResponse(
            JsonResponse(
                tenant));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.GetCurrentAsync();

        Assert.NotNull(
            result);

        Assert.Equal(
            tenant.Id,
            result.Id);

        Assert.Equal(
            tenant.Name,
            result.Name);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            "api/tenants/me");
    }

    [Fact]
    public async Task GetCurrentAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.GetCurrentAsync();

        Assert.Null(
            result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            "api/tenants/me");
    }

    [Fact]
    public async Task UpdateCurrentAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var updatedTenant =
            CreateTenantDto();

        updatedTenant.Name =
            "Sanes Web Actualizado";

        updatedTenant.LegalName =
            "Sanes Web SRL";

        updatedTenant.Phone =
            "8095559999";

        updatedTenant.Email =
            "actualizado@sanes.test";

        updatedTenant.CurrencyCode =
            "DOP";

        updatedTenant.CurrencySymbol =
            "RD$";

        updatedTenant.DefaultLateFeeEnabled =
            true;

        updatedTenant.DefaultLateFeeAmount =
            50m;

        updatedTenant.DefaultLateFeeGraceDays =
            2;

        updatedTenant.GuaranteeRequiredFromAmount =
            1000m;

        apiClient.EnqueueResponse(
            JsonResponse(
                updatedTenant));

        var service =
            CreateService(
                apiClient);

        var request =
            new UpdateTenantRequest
            {
                Name =
                    "Sanes Web Actualizado",

                LegalName =
                    "Sanes Web SRL",

                Phone =
                    "8095559999",

                Email =
                    "actualizado@sanes.test",

                CurrencyCode =
                    "DOP",

                CurrencySymbol =
                    "RD$",

                DefaultLateFeeEnabled =
                    true,

                DefaultLateFeeCalculationType =
                    LateFeeCalculationType
                        .FixedAmountPerInstallment,

                DefaultLateFeeAmount =
                    50m,

                DefaultLateFeeGraceDays =
                    2,

                GuaranteeRequiredFromAmount =
                    1000m
            };

        var result =
            await service.UpdateCurrentAsync(
                request);

        Assert.NotNull(
            result);

        Assert.Equal(
            "Sanes Web Actualizado",
            result.Name);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Put,
            "api/tenants/me");

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.Equal(
            "Sanes Web Actualizado",
            root.GetProperty(
                    "name")
                .GetString());

        Assert.Equal(
            "Sanes Web SRL",
            root.GetProperty(
                    "legalName")
                .GetString());

        Assert.Equal(
            "8095559999",
            root.GetProperty(
                    "phone")
                .GetString());

        Assert.Equal(
            "actualizado@sanes.test",
            root.GetProperty(
                    "email")
                .GetString());

        Assert.Equal(
            "DOP",
            root.GetProperty(
                    "currencyCode")
                .GetString());

        Assert.Equal(
            "RD$",
            root.GetProperty(
                    "currencySymbol")
                .GetString());

        Assert.True(
            root.GetProperty(
                    "defaultLateFeeEnabled")
                .GetBoolean());

        Assert.Equal(
            (int)LateFeeCalculationType
                .FixedAmountPerInstallment,
            root.GetProperty(
                    "defaultLateFeeCalculationType")
                .GetInt32());

        Assert.Equal(
            50m,
            root.GetProperty(
                    "defaultLateFeeAmount")
                .GetDecimal());

        Assert.Equal(
            2,
            root.GetProperty(
                    "defaultLateFeeGraceDays")
                .GetInt32());

        Assert.Equal(
            1000m,
            root.GetProperty(
                    "guaranteeRequiredFromAmount")
                .GetDecimal());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));

        Assert.False(
            root.TryGetProperty(
                "id",
                out _));
    }

    [Fact]
    public async Task UpdateCurrentAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.UpdateCurrentAsync(
                CreateUpdateRequest());

        Assert.Null(
            result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Put,
            "api/tenants/me");
    }

    [Fact]
    public async Task UpdateCurrentAsync_WhenBadRequest_TranslatesMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            ApiError(
                "Late fee amount must be greater than zero when late fees are enabled."));

        var service =
            CreateService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.UpdateCurrentAsync(
                        CreateUpdateRequest()));

        Assert.Equal(
            "El monto de mora debe ser mayor que cero cuando la mora está habilitada.",
            exception.Message);
    }

    [Fact]
    public void TenantSettingsFormModel_FromDto_MapsValues()
    {
        var tenant =
            CreateTenantDto();

        var model =
            TenantSettingsFormModel
                .FromDto(
                    tenant);

        Assert.Equal(
            tenant.Name,
            model.Name);

        Assert.Equal(
            tenant.LegalName,
            model.LegalName);

        Assert.Equal(
            tenant.Phone,
            model.Phone);

        Assert.Equal(
            tenant.Email,
            model.Email);

        Assert.Equal(
            tenant.CurrencyCode,
            model.CurrencyCode);

        Assert.Equal(
            tenant.CurrencySymbol,
            model.CurrencySymbol);

        Assert.Equal(
            tenant.DefaultLateFeeEnabled,
            model.DefaultLateFeeEnabled);

        Assert.Equal(
            tenant.DefaultLateFeeAmount,
            model.DefaultLateFeeAmount);

        Assert.Equal(
            tenant.DefaultLateFeeGraceDays,
            model.DefaultLateFeeGraceDays);

        Assert.Equal(
            tenant.GuaranteeRequiredFromAmount,
            model.GuaranteeRequiredFromAmount);
    }

    [Fact]
    public void TenantSettingsFormModel_ToUpdateRequest_NormalizesValuesAndUsesFixedLateFee()
    {
        var model =
            new TenantSettingsFormModel
            {
                Name =
                    "  Sanes Web  ",

                LegalName =
                    "  Sanes Web SRL  ",

                Phone =
                    "  8095551234  ",

                Email =
                    "  info@sanes.test  ",

                CurrencyCode =
                    "  dop  ",

                CurrencySymbol =
                    "  RD$  ",

                DefaultLateFeeEnabled =
                    true,

                DefaultLateFeeAmount =
                    50m,

                DefaultLateFeeGraceDays =
                    2,

                GuaranteeRequiredFromAmount =
                    1000m
            };

        var request =
            model.ToUpdateRequest();

        Assert.Equal(
            "Sanes Web",
            request.Name);

        Assert.Equal(
            "Sanes Web SRL",
            request.LegalName);

        Assert.Equal(
            "8095551234",
            request.Phone);

        Assert.Equal(
            "info@sanes.test",
            request.Email);

        Assert.Equal(
            "DOP",
            request.CurrencyCode);

        Assert.Equal(
            "RD$",
            request.CurrencySymbol);

        Assert.True(
            request.DefaultLateFeeEnabled);

        Assert.Equal(
            LateFeeCalculationType
                .FixedAmountPerInstallment,
            request.DefaultLateFeeCalculationType);

        Assert.Equal(
            50m,
            request.DefaultLateFeeAmount);

        Assert.Equal(
            2,
            request.DefaultLateFeeGraceDays);

        Assert.Equal(
            1000m,
            request.GuaranteeRequiredFromAmount);
    }

    [Fact]
    public void TenantSettingsFormModel_WhenLateFeeEnabledWithZeroAmount_IsInvalid()
    {
        var model =
            CreateValidFormModel();

        model.DefaultLateFeeEnabled =
            true;

        model.DefaultLateFeeAmount =
            0m;

        var validationResults =
            Validate(
                model);

        Assert.Contains(
            validationResults,
            x =>
                x.ErrorMessage ==
                "El monto de mora debe ser mayor que cero cuando la mora está habilitada.");
    }

    [Fact]
    public void TenantSettingsFormModel_WhenLateFeeAmountNegative_IsInvalid()
    {
        var model =
            CreateValidFormModel();

        model.DefaultLateFeeAmount =
            -1m;

        var validationResults =
            Validate(
                model);

        Assert.Contains(
            validationResults,
            x =>
                x.ErrorMessage ==
                "El monto de mora no puede ser negativo.");
    }

    [Fact]
    public void TenantSettingsFormModel_WhenGraceDaysNegative_IsInvalid()
    {
        var model =
            CreateValidFormModel();

        model.DefaultLateFeeGraceDays =
            -1;

        var validationResults =
            Validate(
                model);

        Assert.Contains(
            validationResults,
            x =>
                x.ErrorMessage ==
                "Los días de gracia no pueden ser negativos.");
    }

    [Fact]
    public void TenantSettingsFormModel_WhenGuaranteeAmountIsZero_IsInvalid()
    {
        var model =
            CreateValidFormModel();

        model.GuaranteeRequiredFromAmount =
            0m;

        var validationResults =
            Validate(
                model);

        Assert.Contains(
            validationResults,
            x =>
                x.ErrorMessage ==
                "El monto mínimo para exigir garantía debe ser mayor que cero.");
    }

    [Fact]
    public void TenantSettingsFormModel_WhenCurrencyCodeNotThreeCharacters_IsInvalid()
    {
        var model =
            CreateValidFormModel();

        model.CurrencyCode =
            "DO";

        var validationResults =
            Validate(
                model);

        Assert.Contains(
            validationResults,
            x =>
                x.MemberNames.Contains(
                    nameof(
                        TenantSettingsFormModel.CurrencyCode)));
    }

    [Fact]
    public void TenantSettingsFormModel_WhenLateFeeDisabledAndGuaranteeNull_IsValid()
    {
        var model =
            CreateValidFormModel();

        model.DefaultLateFeeEnabled =
            false;

        model.DefaultLateFeeAmount =
            0m;

        model.DefaultLateFeeGraceDays =
            0;

        model.GuaranteeRequiredFromAmount =
            null;

        var validationResults =
            Validate(
                model);

        Assert.Empty(
            validationResults);
    }

    private static TenantSettingsWebService
        CreateService(
            TestSanesApiClient apiClient)
    {
        return new TenantSettingsWebService(
            apiClient);
    }

    private static TenantDto
        CreateTenantDto()
    {
        return new TenantDto
        {
            Id =
                Guid.NewGuid(),

            Name =
                "Sanes Web Test",

            LegalName =
                "Sanes Web SRL",

            Phone =
                "8095551234",

            Email =
                "info@sanes.test",

            CurrencyCode =
                "DOP",

            CurrencySymbol =
                "RD$",

            DefaultLateFeeEnabled =
                true,

            DefaultLateFeeCalculationType =
                LateFeeCalculationType
                    .FixedAmountPerInstallment,

            DefaultLateFeeAmount =
                50m,

            DefaultLateFeeGraceDays =
                2,

            GuaranteeRequiredFromAmount =
                1000m,

            IsActive =
                true,

            CreatedAt =
                DateTime.UtcNow.AddDays(-10),

            UpdatedAt =
                DateTime.UtcNow
        };
    }

    private static UpdateTenantRequest
        CreateUpdateRequest()
    {
        return new UpdateTenantRequest
        {
            Name =
                "Sanes Web Test",

            LegalName =
                "Sanes Web SRL",

            Phone =
                "8095551234",

            Email =
                "info@sanes.test",

            CurrencyCode =
                "DOP",

            CurrencySymbol =
                "RD$",

            DefaultLateFeeEnabled =
                true,

            DefaultLateFeeCalculationType =
                LateFeeCalculationType
                    .FixedAmountPerInstallment,

            DefaultLateFeeAmount =
                50m,

            DefaultLateFeeGraceDays =
                2,

            GuaranteeRequiredFromAmount =
                1000m
        };
    }

    private static TenantSettingsFormModel
        CreateValidFormModel()
    {
        return new TenantSettingsFormModel
        {
            Name =
                "Sanes Web Test",

            LegalName =
                "Sanes Web SRL",

            Phone =
                "8095551234",

            Email =
                "info@sanes.test",

            CurrencyCode =
                "DOP",

            CurrencySymbol =
                "RD$",

            DefaultLateFeeEnabled =
                true,

            DefaultLateFeeAmount =
                50m,

            DefaultLateFeeGraceDays =
                2,

            GuaranteeRequiredFromAmount =
                1000m
        };
    }

    private static List<ValidationResult>
        Validate(
            TenantSettingsFormModel model)
    {
        var validationResults =
            new List<ValidationResult>();

        Validator.TryValidateObject(
            model,
            new ValidationContext(
                model),
            validationResults,
            validateAllProperties: true);

        return validationResults;
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

        if (request.Body is not null)
        {
            Assert.False(
                request.Body.Contains(
                    "tenantId",
                    StringComparison.OrdinalIgnoreCase));
        }
    }

    private static HttpResponseMessage
        JsonResponse<T>(
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

    private static HttpResponseMessage
        ApiError(
            string message,
            HttpStatusCode statusCode =
                HttpStatusCode.BadRequest)
    {
        return new HttpResponseMessage(
            statusCode)
        {
            Content =
                JsonContent.Create(
                    new
                    {
                        message
                    })
        };
    }
}