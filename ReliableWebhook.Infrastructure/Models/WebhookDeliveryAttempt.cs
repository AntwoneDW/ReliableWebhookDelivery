namespace ReliableWebhook.Infrastructure.Models;

public class WebhookDeliveryAttempt
{
    public Guid Id { get; set; }

    public Guid WebhookEventId { get; set; }

    public int AttemptNumber { get; set; }

    public int? ResponseStatusCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime AttemptedAtUtc { get; set; }

    public bool Succeeded { get; set; }
}