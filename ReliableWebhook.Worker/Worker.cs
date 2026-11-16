using Microsoft.EntityFrameworkCore;
using ReliableWebhook.Infrastructure.Data;
using ReliableWebhook.Infrastructure.Models;
using ReliableWebhook.Infrastructure.Telemetry;
using System.Diagnostics;

namespace ReliableWebhook.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly WebhookMetrics _metrics;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        WebhookMetrics metrics)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingEvent(stoppingToken);

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
    }

    private async Task ProcessPendingEvent(
        CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<WebhookDbContext>();

        var now = DateTime.UtcNow;

        var webhookEvent = await db.WebhookEvents
            .Where(e =>
                e.DeliveryStatus != "Delivered" &&
                e.DeliveryStatus != "DeadLettered" &&
                (e.NextAttemptAtUtc == null ||
                 e.NextAttemptAtUtc <= now))
            .OrderBy(e => e.CreatedAtUtc)
            .FirstOrDefaultAsync(stoppingToken);

        if (webhookEvent is null)
        {
            _logger.LogDebug("No pending webhook events found");
            return;
        }
        _logger.LogInformation(
    "Processing webhook event {EventId}",
    webhookEvent.Id);

        var endpoint = await db.WebhookEndpoints
            .FirstOrDefaultAsync(
                e => e.Id == webhookEvent.WebhookEndpointId,
                stoppingToken);

        if (endpoint is null || !endpoint.IsActive)
        {
            _logger.LogWarning(
                "Endpoint unavailable for event {EventId}",
                webhookEvent.Id);

            return;
        }

        var previousAttempts =
            await db.WebhookDeliveryAttempts.CountAsync(
                a => a.WebhookEventId == webhookEvent.Id,
                stoppingToken);

        const int maxAttempts = 3;

        var attemptNumber = previousAttempts + 1;
        
        _logger.LogInformation(
            "Processing webhook event {EventId} for endpoint {EndpointId}. Attempt {AttemptNumber}",
            webhookEvent.Id,
            webhookEvent.WebhookEndpointId,
            attemptNumber);

        _metrics.RecordAttempt();
        var stopwatch = Stopwatch.StartNew();

        var attempt = new WebhookDeliveryAttempt
        {
            Id = Guid.NewGuid(),
            WebhookEventId = webhookEvent.Id,
            AttemptNumber = previousAttempts + 1,
            AttemptedAtUtc = DateTime.UtcNow
        };

        try
        {
            var client = _httpClientFactory.CreateClient();

            using var content = new StringContent(
                webhookEvent.Payload,
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                endpoint.Url,
                content,
                stoppingToken);

            attempt.ResponseStatusCode =
                (int)response.StatusCode;

            attempt.Succeeded =
                response.IsSuccessStatusCode;

            stopwatch.Stop();
            _metrics.RecordDeliveryDuration(stopwatch.Elapsed.TotalMilliseconds);

            if (!attempt.Succeeded)
            {
                attempt.ErrorMessage =
                    $"HTTP {(int)response.StatusCode}";
                _metrics.RecordFailure();
                _logger.LogInformation(
                    "Webhook event {EventId} failed on attempt {AttemptNumber} with HTTP {StatusCode}",
                    webhookEvent.Id,
                    attemptNumber,
                    (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            attempt.Succeeded = false;
            attempt.ErrorMessage = ex.Message;
        }

        if (attempt.Succeeded)
        {
            webhookEvent.DeliveryStatus = "Delivered";
            webhookEvent.DeliveredAtUtc = DateTime.UtcNow;
            var totalDeliveryDuration =
                (webhookEvent.DeliveredAtUtc.Value - webhookEvent.CreatedAtUtc)
                .TotalMilliseconds;

            _metrics.RecordTotalDeliveryDuration(totalDeliveryDuration);
            webhookEvent.NextAttemptAtUtc = null;
            _metrics.RecordSuccess();

            _logger.LogInformation(
    "Webhook event {EventId} delivered successfully on attempt {AttemptNumber}. " +
    "Attempt duration {AttemptDurationMs:F2} ms; total delivery duration {TotalDurationMs:F2} ms",
    webhookEvent.Id,
    attemptNumber,
    stopwatch.Elapsed.TotalMilliseconds,
    totalDeliveryDuration);
        }
        else if (attemptNumber >= maxAttempts)
        {
            webhookEvent.DeliveryStatus = "DeadLettered";
            webhookEvent.NextAttemptAtUtc = null;
            _logger.LogError(
                "Webhook event {EventId} moved to dead letter after {AttemptNumber} attempts",
                webhookEvent.Id,
                attemptNumber);
        }
        else
        {
            webhookEvent.DeliveryStatus = "Retrying";
            _logger.LogWarning(
                "Webhook event {EventId} failed on attempt {AttemptNumber}. Next attempt scheduled for {NextAttemptAtUtc}",
                webhookEvent.Id,
                attemptNumber,
                webhookEvent.NextAttemptAtUtc);

            var backoffSeconds = Math.Pow(2, attemptNumber);
            var jitterMilliseconds = Random.Shared.Next(0, 1000);

            webhookEvent.NextAttemptAtUtc =
                DateTime.UtcNow
                    .AddSeconds(backoffSeconds)
                    .AddMilliseconds(jitterMilliseconds);
        }

        db.WebhookDeliveryAttempts.Add(attempt);
        await db.SaveChangesAsync(stoppingToken);

        _logger.LogInformation(
            "Webhook event {EventId}, attempt {AttemptNumber}, success={Succeeded}",
            webhookEvent.Id,
            attempt.AttemptNumber,
            attempt.Succeeded);
    }
}