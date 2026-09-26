using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Sanes.Web.Authentication;
using Sanes.Web.Components;
using Sanes.Web.Configuration;
using Sanes.Web.Api;
using Microsoft.AspNetCore.Antiforgery;
using System.Security.Claims;
using Sanes.Web.Dashboard;
using Sanes.Web.FinancialReports;
using Sanes.Web.Investors;
using Sanes.Web.Clients;
using Sanes.Web.CollectionRoutes;
using Sanes.Web.CollectionRouteSchedules;

var builder = WebApplication.CreateBuilder(args);

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// API configuration
builder.Services
    .AddOptions<ApiOptions>()
    .Bind(
        builder.Configuration.GetSection(
            ApiOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options =>
            Uri.TryCreate(
                options.BaseUrl,
                UriKind.Absolute,
                out _),
        "Api:BaseUrl must be a valid absolute URL.")
    .ValidateOnStart();

// Sanes.Api HttpClient
builder.Services.AddHttpClient(
    "SanesApi",
    (serviceProvider, client) =>
    {
        var apiOptions = serviceProvider
            .GetRequiredService<IOptions<ApiOptions>>()
            .Value;

        client.BaseAddress =
            new Uri(
                apiOptions.BaseUrl,
                UriKind.Absolute);

        client.Timeout =
            TimeSpan.FromSeconds(30);
    });

// Server-side storage for API JWTs.
// For now this uses memory and can later be replaced
// with another IDistributedCache provider.
builder.Services.AddDistributedMemoryCache();

builder.Services.AddScoped<
    IWebSessionStore,
    DistributedWebSessionStore>();

builder.Services.AddScoped<
    IWebAuthenticationService,
    WebAuthenticationService>();

builder.Services.AddScoped<
    ISanesApiClient,
    SanesApiClient>();

builder.Services.AddScoped<
    IFinancialDashboardWebService,
    FinancialDashboardWebService>();

builder.Services.AddScoped<
    IFinancialReportsWebService,
    FinancialReportsWebService>();

builder.Services.AddScoped<
    IInvestorsWebService,
    InvestorsWebService>();

builder.Services.AddScoped<
    IClientsWebService,
    ClientsWebService>();

builder.Services.AddScoped<
    ICollectionRoutesWebService,
    CollectionRoutesWebService>();

builder.Services.AddScoped<
    ICollectionRouteSchedulesWebService,
    CollectionRouteSchedulesWebService>();

// Web authentication
builder.Services
    .AddAuthentication(
        WebAuthConstants.AuthenticationScheme)
    .AddCookie(
        WebAuthConstants.AuthenticationScheme,
        options =>
        {
            options.Cookie.Name =
                "Sanes.Auth";

            options.Cookie.HttpOnly =
                true;

            options.Cookie.SameSite =
                SameSiteMode.Lax;

            options.Cookie.SecurePolicy =
                CookieSecurePolicy.SameAsRequest;

            options.LoginPath =
                "/login";

            options.AccessDeniedPath =
                "/forbidden";

            options.SlidingExpiration =
                false;

            options.Events.OnValidatePrincipal =
                async context =>
                {
                    var sessionId =
                        context.Principal?
                            .FindFirst(
                                WebAuthConstants.SessionIdClaim)?
                            .Value;

                    if (string.IsNullOrWhiteSpace(sessionId))
                    {
                        context.RejectPrincipal();

                        await context.HttpContext.SignOutAsync(
                            WebAuthConstants.AuthenticationScheme);

                        return;
                    }

                    var sessionStore =
                        context.HttpContext
                            .RequestServices
                            .GetRequiredService<IWebSessionStore>();

                    var session =
                        await sessionStore.GetAsync(
                            sessionId,
                            context.HttpContext.RequestAborted);

                    if (session is null)
                    {
                        context.RejectPrincipal();

                        await context.HttpContext.SignOutAsync(
                            WebAuthConstants.AuthenticationScheme);
                    }
                };
        });

builder.Services.AddAuthorization();

builder.Services
    .AddCascadingAuthenticationState();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapPost(
    "/auth/logout",
    async (
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IWebSessionStore sessionStore) =>
    {
        await antiforgery.ValidateRequestAsync(
            httpContext);

        var sessionId =
            httpContext.User
                .FindFirst(
                    WebAuthConstants.SessionIdClaim)?
                .Value;

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await sessionStore.RemoveAsync(
                sessionId,
                httpContext.RequestAborted);
        }

        await httpContext.SignOutAsync(
            WebAuthConstants.AuthenticationScheme);

        return Results.Redirect(
            "/login");
    })
    .RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();