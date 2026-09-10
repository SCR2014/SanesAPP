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



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SanesDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

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

builder.Services.AddScoped<ICollectionRouteRepository, CollectionRouteRepository>();
builder.Services.AddScoped<ICollectionRouteService, CollectionRouteService>();

builder.Services.AddScoped<ICollectionRouteRepository, CollectionRouteRepository>();

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

app.UseHttpsRedirection();

app.MapControllers();

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