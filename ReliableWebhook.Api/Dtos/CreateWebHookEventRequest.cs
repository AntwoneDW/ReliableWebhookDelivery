namespace ReliableWebhook.Api.Dtos;

public class CreateWebhookEventRequest
{
    public Guid WebhookEndpointId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;
}