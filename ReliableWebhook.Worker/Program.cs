using Microsoft.EntityFrameworkCore;
using ReliableWebhook.Infrastructure.Data;
using ReliableWebhook.Worker;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddConsoleExporter());

var host = builder.Build();

Console.WriteLine(
    $"Environment: {builder.Environment.EnvironmentName}");

Console.WriteLine(
    $"Connection string exists: " +
    $"{builder.Configuration.GetConnectionString("WebhookDb") is not null}");


var connectionString =
    builder.Configuration.GetConnectionString("WebhookDb")
    ?? throw new InvalidOperationException(
        "Connection string 'WebhookDb' was not configured."); 
host.Run();