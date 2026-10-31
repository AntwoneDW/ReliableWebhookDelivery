namespace ReliableWebhook.Api.Dtos;

public class WebhookEndpointResponse
{
    public Guid Id { get; set; }

    public string Url { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public bool IsActive { get; set; }
}