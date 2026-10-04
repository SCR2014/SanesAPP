using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Sanes.Application.Clients.Repositories;
using Sanes.Application.CollectionRoutes.Repositories;
using Sanes.Application.Tenants.Repositories;
using Sanes.Application.Tenants.Services;
using Sanes.Infrastructure.Clients.Repositories;
using Sanes.Infrastructure.CollectionRoutes.Repositories;
using Sanes.Application.Loans.Repositories;
using Sanes.Application.Payments.Repositories;
using Sanes.Infrastructure.Loans.Repositories;
using Sanes.Infrastructure.Payments.Repositories;
using Sanes.Infrastructure.Persistence;
using Sanes.Infrastructure.Tenants.Repositories;
using Sanes.Application.Investors.Repositories;
using Sanes.Infrastructure.Investors.Repositories;
using Sanes.Application.Investors.Services;
using Sanes.Application.Clients.Services;
using Sanes.Application.Loans.Services;
using Sanes.Application.Payments.Services;
using Sanes.Application.CollectionRoutes.Services;
using Sanes.Application.AppUsers.Repositories;
using Sanes.Infrastructure.AppUsers.Repositories;
using Sanes.Application.AppUsers.Services;
using Sanes.Application.CollectionRouteSchedules.Repositories;
using Sanes.Infrastructure.CollectionRouteSchedules.Repositories;
using Sanes.Application.CollectionRouteSchedules.Services;
using Sanes.Application.CollectionAgenda.Repositories;
using Sanes.Infrastructure.CollectionAgenda.Repositories;
using Sanes.Application.CollectionAgenda.Services;
using Sanes.Application.FieldCollections.Services;
using Sanes.Application.Authentication.Services;
using Sanes.Infrastructure.Authentication.Services;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Sanes.Infrastructure.Authentication;
using Sanes.Api.Authentication;
using Sanes.Application.Provisioning.Services;
using Sanes.Infrastructure.Provisioning;
using Sanes.Infrastructure.Provisioning.Services;
using Sanes.Application.LateFees.Repositories;
using Sanes.Infrastructure.LateFees.Repositories;
using Sanes.Application.LateFees.Services;
using Sanes.Application.Common.Persistence;
using Sanes.Application.Common.Files;
using Sanes.Infrastructure.Files;
using Sanes.Application.Dashboard.Repositories;
using Sanes.Infrastructure.Dashboard.Repositories;
using Sanes.Application.Dashboard.Services;
using Sanes.Application.FinancialReports.Repositories;
using Sanes.Infrastructure.FinancialReports.Repositories;
using Sanes.Application.FinancialReports.Services;
using Sanes.Application.FinancialReports.Exports;
using Sanes.Infrastructure.FinancialReports.Exports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(
    options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto;

        options.KnownIPNetworks.Add(
            new System.Net.IPNetwork(
                IPAddress.Parse("::ffff:10.0.0.0"),
                104));

        options.KnownIPNetworks.Add(
            new System.Net.IPNetwork(
                IPAddress.Parse("::ffff:172.16.0.0"),
                108));

        options.KnownIPNetworks.Add(
            new System.Net.IPNetwork(
                IPAddress.Parse("::ffff:192.168.0.0"),
                112));
    });

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(
        JwtSettings.SectionName));

var jwtSettings =
    builder.Configuration
        .GetSection(JwtSettings.SectionName)
        .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException(
        "JWT signing key is not configured.");
}

builder.Services.AddDbContext<SanesDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<SanesDbContext>(
        name: "postgresql",
        tags:
        [
            "ready"
        ]);

var applicationInsightsConnectionString =
    builder.Configuration[
        "APPLICATIONINSIGHTS_CONNECTION_STRING"];

if (!string.IsNullOrWhiteSpace(
        applicationInsightsConnectionString))
{
    builder.Services
        .AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString =
                applicationInsightsConnectionString;
        });
}

builder.Services.AddScoped<
    ITransactionRunner,
    EfTransactionRunner>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    jwtSettings.Issuer,

                ValidAudience =
                    jwtSettings.Audience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSettings.Key)),

                ClockSkew =
                    TimeSpan.Zero,

                NameClaimType =
                    ClaimTypes.Name,

                RoleClaimType =
                    ClaimTypes.Role
            };

        options.Events =
            new JwtBearerEvents
            {
                OnTokenValidated =
                    async context =>
                    {
                        var principal =
                            context.Principal;

                        if (principal is null)
                        {
                            context.Fail(
                                "Invalid authentication token.");

                            return;
                        }

                        var tenantIdValue =
                            principal.FindFirstValue(
                                "tenant_id");

                        var appUserIdValue =
                            principal.FindFirstValue(
                                ClaimTypes.NameIdentifier)
                            ??
                            principal.FindFirstValue(
                                "sub");

                        var roleValue =
                            principal.FindFirstValue(
                                ClaimTypes.Role);

                        if (!Guid.TryParse(
                                tenantIdValue,
                                out var tenantId)
                            ||
                            !Guid.TryParse(
                                appUserIdValue,
                                out var appUserId)
                            ||
                            string.IsNullOrWhiteSpace(
                                roleValue))
                        {
                            context.Fail(
                                "Invalid authentication token.");

                            return;
                        }

                        var dbContext =
                            context.HttpContext
                                .RequestServices
                                .GetRequiredService<
                                    SanesDbContext>();

                        var tenantIsActive =
                            await dbContext.Tenants
                                .AsNoTracking()
                                .AnyAsync(
                                    x =>
                                        x.Id == tenantId
                                        &&
                                        x.IsActive,
                                    context.HttpContext
                                        .RequestAborted);

                        if (!tenantIsActive)
                        {
                            context.Fail(
                                "Authentication principal is inactive.");

                            return;
                        }

                        var appUser =
                            await dbContext.AppUsers
                                .AsNoTracking()
                                .Where(
                                    x =>
                                        x.Id == appUserId
                                        &&
                                        x.TenantId == tenantId
                                        &&
                                        x.IsActive)
                                .Select(
                                    x => new
                                    {
                                        x.Role
                                    })
                                .SingleOrDefaultAsync(
                                    context.HttpContext
                                        .RequestAborted);

                        if (appUser is null)
                        {
                            context.Fail(
                                "Authentication principal is inactive.");

                            return;
                        }

                        if (!string.Equals(
                                appUser.Role.ToString(),
                                roleValue,
                                StringComparison.Ordinal))
                        {
                            context.Fail(
                                "Authentication principal has changed.");
                        }
                    }
            };
    });

builder.Services.Configure<ProvisioningSettings>(
    builder.Configuration.GetSection(
        ProvisioningSettings.SectionName));   

builder.Services.AddAuthorization();

builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ITenantService, TenantService>();

builder.Services.AddScoped<IInvestorRepository, InvestorRepository>();
builder.Services.AddScoped<IInvestorService, InvestorService>();

builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IClientService, ClientService>();

builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<ILoanService, LoanService>();

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<
    IPaymentIdempotencyRepository,
    PaymentIdempotencyRepository>();

builder.Services.AddScoped<
    IPaymentReversalRepository,
    PaymentReversalRepository>();
builder.Services.AddScoped<
    IPaymentReversalService,
    PaymentReversalService>();

builder.Services.AddScoped<
    IPaymentAllocationRepository,
    PaymentAllocationRepository>();

builder.Services.AddScoped<ILateFeeRepository, LateFeeRepository>();
builder.Services.AddScoped<
    ILateFeeAccrualService,
    LateFeeAccrualService>();

builder.Services.AddScoped<
    ILateFeeBalanceService,
    LateFeeBalanceService>();

builder.Services.AddScoped<
    ILateFeeAdministrationService,
    LateFeeAdministrationService>();

builder.Services.AddScoped<ICollectionRouteRepository, CollectionRouteRepository>();
builder.Services.AddScoped<ICollectionRouteService, CollectionRouteService>();

builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();
builder.Services.AddScoped<IAppUserService, AppUserService>();

builder.Services.AddScoped<IAppUserCollectionRouteRepository,AppUserCollectionRouteRepository>();

builder.Services.AddScoped<
    ICollectionRouteScheduleRepository,
    CollectionRouteScheduleRepository>();
builder.Services.AddScoped<
    ICollectionRouteScheduleService,
    CollectionRouteScheduleService>();

builder.Services.AddScoped<
    ICollectionAgendaRepository,
    CollectionAgendaRepository>();
builder.Services.AddScoped<
    ICollectionAgendaService,
    CollectionAgendaService>();

builder.Services.AddScoped<
    IFieldCollectionService,
    FieldCollectionService>();

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<
    IProvisioningService,
    ProvisioningService>();

builder.Services.AddScoped<
    ICurrentUserService,
    CurrentUserService>();

builder.Services.AddScoped<
    ILoanBalanceAdjustmentRepository,
    LoanBalanceAdjustmentRepository>();

builder.Services.AddScoped<
    IEarlySettlementRepository,
    EarlySettlementRepository>();
builder.Services.AddScoped<
    IEarlySettlementService,
    EarlySettlementService>();

builder.Services.AddScoped<
    ILoanGuaranteeService,
    LoanGuaranteeService>();

builder.Services.AddScoped<
    ILoanGuaranteeAttachmentRepository,
    LoanGuaranteeAttachmentRepository>();
builder.Services.AddScoped<
    ILoanGuaranteeAttachmentService,
    LoanGuaranteeAttachmentService>();

builder.Services.AddScoped<
    IPaymentReceiptRepository,
    PaymentReceiptRepository>();
builder.Services.AddScoped<
    IPaymentReceiptService,
    PaymentReceiptService>();

builder.Services.AddScoped<
    IFinancialDashboardRepository,
    FinancialDashboardRepository>();
builder.Services.AddScoped<
    IFinancialDashboardService,
    FinancialDashboardService>();

builder.Services.AddScoped<
    IFinancialReportRepository,
    FinancialReportRepository>();
builder.Services.AddScoped<
    IFinancialReportService,
    FinancialReportService>();

builder.Services.AddScoped<
    IFinancialReportExportService,
    ExcelFinancialReportExportService>();

builder.Services.AddSingleton<IFileStorage>(
    _ =>
    {
        var provider =
            builder.Configuration[
                "FileStorage:Provider"];

        if (string.Equals(
                provider,
                "AzureBlob",
                StringComparison.OrdinalIgnoreCase))
        {
            var accountName =
                builder.Configuration[
                    "FileStorage:AccountName"];

            var containerName =
                builder.Configuration[
                    "FileStorage:ContainerName"];

            if (string.IsNullOrWhiteSpace(
                    accountName))
            {
                throw new InvalidOperationException(
                    "FileStorage:AccountName is required when FileStorage:Provider is AzureBlob.");
            }

            if (string.IsNullOrWhiteSpace(
                    containerName))
            {
                throw new InvalidOperationException(
                    "FileStorage:ContainerName is required when FileStorage:Provider is AzureBlob.");
            }

            var serviceUri =
                new Uri(
                    $"https://{accountName}.blob.core.windows.net");

            var credential =
                new Azure.Identity
                    .DefaultAzureCredential();

            var blobServiceClient =
                new Azure.Storage.Blobs
                    .BlobServiceClient(
                        serviceUri,
                        credential);

            var containerClient =
                blobServiceClient
                    .GetBlobContainerClient(
                        containerName);

            return new AzureBlobFileStorage(
                containerClient);
        }

        if (
            !string.IsNullOrWhiteSpace(provider) &&
            !string.Equals(
                provider,
                "Local",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported FileStorage provider '{provider}'. Supported values are Local and AzureBlob.");
        }

        var configuredRoot =
            builder.Configuration[
                "FileStorage:RootPath"];

        var rootPath =
            string.IsNullOrWhiteSpace(
                configuredRoot)
                ? Path.Combine(
                    builder.Environment
                        .ContentRootPath,
                    "data")
                : Path.GetFullPath(
                    configuredRoot);

        return new LocalFileStorage(
            rootPath);
    });
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate =
            _ => false
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate =
            healthCheckRegistration =>
                healthCheckRegistration
                    .Tags
                    .Contains(
                        "ready")
    });

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild",
    "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF =>
        32 + (int)(TemperatureC / 0.5556);
}

public partial class Program { }