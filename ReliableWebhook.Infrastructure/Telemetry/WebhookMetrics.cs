using System.Diagnostics.Metrics;

namespace ReliableWebhook.Infrastructure.Telemetry;

public sealed class WebhookMetrics
{
    private readonly Histogram<double> _totalDeliveryDuration;
    private readonly Histogram<double> _deliveryAttemptDuration;
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

        _deliveryAttemptDuration = meter.CreateHistogram<double>(
    "webhook.delivery.attempt.duration",
    unit: "ms",
    description: "Duration of outbound webhook delivery attempts");

        _totalDeliveryDuration = meter.CreateHistogram<double>(
    "webhook.delivery.total.duration",
    unit: "ms",
    description: "Time from webhook event creation to successful delivery");
    }

    public void RecordTotalDeliveryDuration(double milliseconds)
    {
        _totalDeliveryDuration.Record(milliseconds);
    }

    public void RecordDeliveryDuration(double milliseconds)
    {
        _deliveryAttemptDuration.Record(milliseconds);
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