using ReliableWebhook.Infrastructure.Models;
using ReliableWebhook.Infrastructure.Services;
using Xunit;

namespace ReliableWebhook.Tests;

public class WebhookDeliveryPolicyTests
{
    [Fact]
    public void SuccessfulDelivery_MarksEventDelivered()
    {
        var webhookEvent = new WebhookEvent
        {
            Id = Guid.NewGuid(),
            DeliveryStatus = "Pending"
        };

        var now = DateTime.UtcNow;

        WebhookDeliveryPolicy.ApplyResult(
            webhookEvent,
            succeeded: true,
            attemptNumber: 1,
            maxAttempts: 3,
            nowUtc: now);

        Assert.Equal(
            "Delivered",
            webhookEvent.DeliveryStatus);

        Assert.Equal(
            now,
            webhookEvent.DeliveredAtUtc);

        Assert.Null(
            webhookEvent.NextAttemptAtUtc);
    }

    [Fact]
    public void ThirdFailedAttempt_MarksEventDeadLettered()
    {
        var webhookEvent = new WebhookEvent
        {
            Id = Guid.NewGuid(),
            DeliveryStatus = "Retrying"
        };

        WebhookDeliveryPolicy.ApplyResult(
            webhookEvent,
            succeeded: false,
            attemptNumber: 3,
            maxAttempts: 3,
            nowUtc: DateTime.UtcNow);

        Assert.Equal(
            "DeadLettered",
            webhookEvent.DeliveryStatus);

        Assert.Null(
            webhookEvent.NextAttemptAtUtc);
    }
}