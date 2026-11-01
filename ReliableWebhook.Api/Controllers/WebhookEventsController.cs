using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReliableWebhook.Infrastructure.Data;
using ReliableWebhook.Api.Dtos;
using ReliableWebhook.Infrastructure.Models;

namespace ReliableWebhook.Api.Controllers;

[ApiController]
[Route("api/events")]
public class WebhookEventsController : ControllerBase
{
    private readonly WebhookDbContext _db;

    public WebhookEventsController(WebhookDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<WebhookEventResponse>> Create(
        CreateWebhookEventRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest("Idempotency-Key header is required.");
        }

        var endpointExists =
            await _db.WebhookEndpoints.AnyAsync(
                x => x.Id == request.WebhookEndpointId);

        if (!endpointExists)
        {
            return BadRequest("Webhook endpoint does not exist.");
        }

        var existing =
            await _db.WebhookEvents
                .FirstOrDefaultAsync(
                    x => x.IdempotencyKey == idempotencyKey);

        if (existing is not null)
        {
            return Ok(ToResponse(existing));
        }

        var webhookEvent = new WebhookEvent
        {
            Id = Guid.NewGuid(),
            WebhookEndpointId = request.WebhookEndpointId,
            EventType = request.EventType,
            Payload = request.Payload,
            IdempotencyKey = idempotencyKey,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.WebhookEvents.Add(webhookEvent);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Protects against two concurrent requests racing
            // with the same idempotency key.
            var concurrentExisting =
                await _db.WebhookEvents
                    .FirstOrDefaultAsync(
                        x => x.IdempotencyKey == idempotencyKey);

            if (concurrentExisting is not null)
            {
                return Ok(ToResponse(concurrentExisting));
            }

            throw;
        }

        return Created(
            $"/api/events/{webhookEvent.Id}",
            ToResponse(webhookEvent));
    }

    private static WebhookEventResponse ToResponse(
        WebhookEvent webhookEvent)
    {
        return new WebhookEventResponse
        {
            Id = webhookEvent.Id,
            WebhookEndpointId = webhookEvent.WebhookEndpointId,
            EventType = webhookEvent.EventType,
            Payload = webhookEvent.Payload,
            CreatedAtUtc = webhookEvent.CreatedAtUtc
        };
    }
}