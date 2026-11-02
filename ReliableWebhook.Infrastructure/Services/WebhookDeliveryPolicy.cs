using ReliableWebhook.Infrastructure.Models;

namespace ReliableWebhook.Infrastructure.Services;

public static class WebhookDeliveryPolicy
{
    public static void ApplyResult(
        WebhookEvent webhookEvent,
        bool succeeded,
        int attemptNumber,
        int maxAttempts,
        DateTime nowUtc,
        int jitterMilliseconds = 0)
    {
        if (succeeded)
        {
            webhookEvent.DeliveryStatus = "Delivered";
            webhookEvent.DeliveredAtUtc = nowUtc;
            webhookEvent.NextAttemptAtUtc = null;
            return;
        }

        if (attemptNumber >= maxAttempts)
        {
            webhookEvent.DeliveryStatus = "DeadLettered";
            webhookEvent.NextAttemptAtUtc = null;
            return;
        }

        webhookEvent.DeliveryStatus = "Retrying";

        var backoffSeconds = Math.Pow(2, attemptNumber);

        webhookEvent.NextAttemptAtUtc =
            nowUtc
                .AddSeconds(backoffSeconds)
                .AddMilliseconds(jitterMilliseconds);
    }
}