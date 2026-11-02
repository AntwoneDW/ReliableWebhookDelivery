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
        Console.WriteLine("GETTING STATUS FOR webhookEvent.Id: " + webhookEvent.Id);
        return new WebhookEventResponse
        {
            Id = webhookEvent.Id,
            WebhookEndpointId = webhookEvent.WebhookEndpointId,
            EventType = webhookEvent.EventType,
            Payload = webhookEvent.Payload,
            CreatedAtUtc = webhookEvent.CreatedAtUtc
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WebhookEventStatusResponse>> Get(Guid id)
    {
        var webhookEvent = await _db.WebhookEvents
            .FirstOrDefaultAsync(x => x.Id == id);

        if (webhookEvent is null)
        {
            Console.WriteLine("GET: I ain't got that in here: " + id);
            return NotFound();
        }

        var attempts = await _db.WebhookDeliveryAttempts
            .Where(a => a.WebhookEventId == id)
            .OrderBy(a => a.AttemptNumber)
            .ToListAsync();

        return Ok(new WebhookEventStatusResponse
        {
            Id = webhookEvent.Id,
            EventType = webhookEvent.EventType,
            DeliveryStatus = webhookEvent.DeliveryStatus,
            CreatedAtUtc = webhookEvent.CreatedAtUtc,
            DeliveredAtUtc = webhookEvent.DeliveredAtUtc,
            NextAttemptAtUtc = webhookEvent.NextAttemptAtUtc,

            Attempts = attempts.Select(a =>
                new WebhookDeliveryAttemptResponse
                {
                    AttemptNumber = a.AttemptNumber,
                    ResponseStatusCode = a.ResponseStatusCode,
                    Succeeded = a.Succeeded,
                    ErrorMessage = a.ErrorMessage,
                    AttemptedAtUtc = a.AttemptedAtUtc
                })
                .ToList()
        });
    }
}