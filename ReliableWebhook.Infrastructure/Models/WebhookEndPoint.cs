namespace ReliableWebhook.Infrastructure.Models;

public class WebhookEndpoint
{
    public Guid Id { get; set; }

    public string Url { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public bool IsActive { get; set; }
}