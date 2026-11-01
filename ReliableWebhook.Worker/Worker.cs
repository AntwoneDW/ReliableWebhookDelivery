using Microsoft.EntityFrameworkCore;
using ReliableWebhook.Infrastructure.Data;
using ReliableWebhook.Infrastructure.Models;

namespace ReliableWebhook.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
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

        var webhookEvent = await db.WebhookEvents
            .Where(e =>
                !db.WebhookDeliveryAttempts.Any(a =>
                    a.WebhookEventId == e.Id &&
                    a.Succeeded))
            .OrderBy(e => e.CreatedAtUtc)
            .FirstOrDefaultAsync(stoppingToken);

        if (webhookEvent is null)
        {
            return;
        }

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

            if (!attempt.Succeeded)
            {
                attempt.ErrorMessage =
                    $"HTTP {(int)response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            attempt.Succeeded = false;
            attempt.ErrorMessage = ex.Message;
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