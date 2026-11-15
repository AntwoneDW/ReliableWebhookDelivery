using System.Diagnostics.Metrics;

namespace ReliableWebhook.Infrastructure.Telemetry;

public sealed class WebhookMetrics
{
    private readonly Counter<long> _deliveryAttempts;
    private readonly Counter<long> _deliverySuccesses;
    private readonly Counter<long> _deliveryFailures;

    public WebhookMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("ReliableWebhook");

        _deliveryAttempts = meter.CreateCounter<long>(
            "webhook.delivery.attempts");

        _deliverySuccesses = meter.CreateCounter<long>(
            "webhook.delivery.successes");

        _deliveryFailures = meter.CreateCounter<long>(
            "webhook.delivery.failures");
    }

    public void RecordAttempt()
    {
        _deliveryAttempts.Add(1);
    }

    public void RecordSuccess()
    {
        _deliverySuccesses.Add(1);
    }

    public void RecordFailure()
    {
        _deliveryFailures.Add(1);
    }
}