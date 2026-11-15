using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ReliableWebhook.Infrastructure.Data;
using ReliableWebhook.Infrastructure.Models;
using ReliableWebhook.Infrastructure.Telemetry;
using ReliableWebhook.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<WebhookDbContext>(options =>
 options.UseSqlServer(
    builder.Configuration.GetConnectionString("WebhookDb"),
    sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));


builder.Services.AddHttpClient();

builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<WebhookMetrics>();

const string serviceName = "ReliableWebhook.Worker";

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(serviceName))
    .WithTracing(tracing =>
        tracing
            .AddHttpClientInstrumentation()
            .AddConsoleExporter())
    .WithMetrics(metrics =>
        metrics
            .AddMeter("ReliableWebhook")
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddConsoleExporter());

var host = builder.Build();

var _logger = host.Services.GetRequiredService<ILogger<Program>>();

_logger.LogWarning($"Environment: {builder.Environment.EnvironmentName}");

_logger.LogWarning($"Connection string exists: " +
    $"{builder.Configuration.GetConnectionString("WebhookDb") is not null}");


var connectionString =
    builder.Configuration.GetConnectionString("WebhookDb")
    ?? throw new InvalidOperationException(
        "Connection string 'WebhookDb' was not configured."); 
host.Run();