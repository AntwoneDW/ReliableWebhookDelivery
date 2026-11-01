using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReliableWebhook.Infrastructure.Data;
using ReliableWebhook.Api.Dtos;
using ReliableWebhook.Infrastructure.Models;

namespace ReliableWebhook.Api.Controllers;

[ApiController]
[Route("api/endpoints")]
public class WebhookEndpointsController : ControllerBase
{
    private readonly WebhookDbContext _db;

    public WebhookEndpointsController(WebhookDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<WebhookEndpointResponse>> Create(
        CreateWebhookEndpointRequest request)
    {
        var endpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            Url = request.Url,
            Secret = request.Secret,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        _db.WebhookEndpoints.Add(endpoint);
        await _db.SaveChangesAsync();

        var response = new WebhookEndpointResponse
        {
            Id = endpoint.Id,
            Url = endpoint.Url,
            CreatedAtUtc = endpoint.CreatedAtUtc,
            IsActive = endpoint.IsActive
        };

        return Created(
            $"/api/endpoints/{endpoint.Id}",
            response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WebhookEndpointResponse>> Get(Guid id)
    {
        var endpoint = await _db.WebhookEndpoints
            .FirstOrDefaultAsync(x => x.Id == id);

        if (endpoint is null)
        {
            return NotFound();
        }

        return Ok(new WebhookEndpointResponse
        {
            Id = endpoint.Id,
            Url = endpoint.Url,
            CreatedAtUtc = endpoint.CreatedAtUtc,
            IsActive = endpoint.IsActive
        });
    }
}