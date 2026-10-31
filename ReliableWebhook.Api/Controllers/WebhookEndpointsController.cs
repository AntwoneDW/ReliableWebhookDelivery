using Microsoft.AspNetCore.Mvc;
using ReliableWebhook.Api.Dtos;
using ReliableWebhook.Api.Models;

namespace ReliableWebhook.Api.Controllers;

[ApiController]
[Route("api/endpoints")]
public class WebhookEndpointsController : ControllerBase
{
    [HttpPost]
    public ActionResult<WebhookEndpointResponse> Create(
        CreateWebhookEndpointRequest request)
    {
        var endpoint = new WebhookEndpointResponse
        {
            Id = Guid.NewGuid(),
            Url = request.Url,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        return Created(
            $"/api/endpoints/{endpoint.Id}",
            endpoint);
    }
}