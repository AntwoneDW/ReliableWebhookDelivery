namespace ReliableWebhook.Api.Dtos;

public class WebhookEventResponse
{
    public Guid Id { get; set; }
    public Guid WebhookEndpointId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}