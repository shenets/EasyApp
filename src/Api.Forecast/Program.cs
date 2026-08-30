using System;
using Api.Forecast.Application.Endpoints;
using Api.Forecast.Application.Middleware;
using Api.Forecast.Application.Services;
using Api.Forecast.Domain.Services;
using Api.Forecast.Infrastructure;
using Api.Forecast.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace('+', '.') ?? type.Name);
});

var forecastDbConnection = builder.Configuration.GetConnectionString("forecast") ?? "Data Source=forecast.db";
builder.Services.AddDbContext<ForecastDbContext>(options => options.UseSqlite(forecastDbConnection));

builder.Services.AddSingleton<IHighPerfParser, HighPerfParser>();

builder.Services.AddScoped<PredictionService>();
builder.Services.AddScoped<IForecastDomainService, PredictionService>();
builder.Services.AddScoped(provider => new Lazy<PredictionService>(() => provider.GetRequiredService<PredictionService>()));
builder.Services.AddScoped<ForecastService>();
builder.Services.AddScoped<IForecastDomainService, ForecastService>();
builder.Services.AddScoped(provider => new Lazy<ForecastService>(() => provider.GetRequiredService<ForecastService>()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ForecastDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapGet("/", () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", forecast = "running" }));

app.UseWhen(
    context => !context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase),
    appBuilder => appBuilder.UseHttpsRedirection());

app.UseMiddleware<ZeroAllocLoggingMiddleware>();

HighPerfEndpoints.Map(app);

app.MapControllers();

// Run the application
app.Run();
