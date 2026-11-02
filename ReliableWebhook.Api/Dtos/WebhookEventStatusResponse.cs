public class WebhookEventStatusResponse
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string DeliveryStatus { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }

    public List<WebhookDeliveryAttemptResponse> Attempts { get; set; } = [];
}

public class WebhookDeliveryAttemptResponse
{
    public int AttemptNumber { get; set; }
    public int? ResponseStatusCode { get; set; }
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AttemptedAtUtc { get; set; }
}