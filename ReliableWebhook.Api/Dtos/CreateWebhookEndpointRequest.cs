namespace ReliableWebhook.Api.Dtos;

public class CreateWebhookEndpointRequest
{
    public string Url { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;
}