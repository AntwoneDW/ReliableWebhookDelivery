# Reliable Webhook Delivery Service

A production-oriented webhook delivery service built with modern .NET to demonstrate reliable event ingestion, asynchronous delivery, retry behavior, failure handling, containerization, automated testing, CI, and observability.

This project is maintained as an internal engineering and portfolio project under **Antwone Software LLC**.

## Overview

The service accepts webhook events through a REST API, persists them, and delivers them asynchronously through a background Worker.

The design focuses on reliability concerns that appear in real distributed systems:

- idempotent event ingestion
- database-backed duplicate protection
- asynchronous background processing
- persisted delivery attempts
- exponential backoff
- retry jitter
- dead-letter handling
- delivery status/history
- health checks
- structured logging
- OpenTelemetry traces and metrics
- automated build/test validation
- containerized local deployment

## Technology Stack

- .NET 10
- ASP.NET Core
- Entity Framework Core
- SQL Server
- .NET Worker Service / BackgroundService
- HttpClient
- xUnit
- SQLite for relational test scenarios
- Docker
- Docker Compose
- GitHub Actions
- OpenTelemetry
- OpenTelemetry Collector

## Solution Structure

```text
ReliableWebhook.Api
    REST API for endpoint registration, event ingestion,
    delivery status, health checks, and test receivers

ReliableWebhook.Worker
    Background worker responsible for asynchronous
    webhook delivery, retries, and failure handling

ReliableWebhook.Infrastructure
    Shared persistence, EF Core models, migrations,
    delivery policy, and telemetry components

ReliableWebhook.Tests
    Automated tests covering reliability behavior
```

## Architecture

```text
                         ┌──────────────────────────┐
                         │          Client          │
                         └─────────────┬────────────┘
                                       │
                                       ▼
                         ┌──────────────────────────┐
                         │   ReliableWebhook.Api    │
                         └─────────────┬────────────┘
                                       │
                                       ▼
                         ┌──────────────────────────┐
                         │       SQL Server         │
                         └─────────────┬────────────┘
                                       │
                                       ▼
                         ┌──────────────────────────┐
                         │ ReliableWebhook.Worker   │
                         └─────────────┬────────────┘
                                       │
                                       ▼
                         ┌──────────────────────────┐
                         │   Target Webhook URL     │
                         └──────────────────────────┘


              API + Worker
                   │
                   │ OTLP
                   ▼
        ┌──────────────────────────────┐
        │ OpenTelemetry Collector      │
        └──────────────────────────────┘
```

## Reliability Features

### Idempotent Event Ingestion

Webhook events are submitted with an `Idempotency-Key` HTTP header.

The database enforces uniqueness so duplicate submissions return the previously created event rather than creating duplicate delivery work.

This protects against both ordinary client retries and concurrent duplicate requests.

### Asynchronous Delivery

Incoming events are persisted before delivery.

The Worker independently polls for pending events and performs outbound webhook delivery using `HttpClient`.

This keeps request ingestion independent from downstream webhook availability.

### Persisted Delivery Attempts

Each delivery attempt records information including:

- attempt number
- HTTP status code
- success/failure state
- error information
- attempt timestamp

Attempt history remains available after the event reaches its final state.

### Retry, Backoff, and Jitter

Failed deliveries are retried using exponential backoff with randomized jitter.

This reduces the risk of synchronized retry storms when a downstream dependency is unavailable.

### Dead-Letter Handling

After the configured retry limit is exhausted, the event transitions to a `DeadLettered` state rather than retrying indefinitely.

### Delivery States

Events progress through delivery states including:

```text
Pending
Retrying
Delivered
DeadLettered
```

## Observability

The application uses structured logging and OpenTelemetry instrumentation.

Framework logging is intentionally reduced so operational application events remain easier to identify.

### Custom Metrics

The Worker emits application-specific metrics including:

```text
webhook.delivery.attempts

webhook.delivery.successes

webhook.delivery.failures

webhook.delivery.attempt.duration

webhook.delivery.total.duration
```

`webhook.delivery.attempt.duration` measures the duration of an individual outbound webhook attempt.

`webhook.delivery.total.duration` measures end-to-end successful delivery latency from event creation until final delivery.

This distinction allows the system to separate downstream HTTP performance from total business-level delivery latency.

### Tracing

OpenTelemetry instrumentation captures incoming ASP.NET Core requests and outbound `HttpClient` activity.

### OTLP / Collector

Telemetry is exported using OTLP to an OpenTelemetry Collector.

```text
Application
    ↓
OTLP
    ↓
OpenTelemetry Collector
    ↓
Configured telemetry backend/exporter
```

The current development configuration uses the Collector debug exporter to verify telemetry flow.

The design allows additional observability backends to be introduced later without coupling application instrumentation directly to a specific vendor.

## Running with Docker Compose

Create a local `.env` file in the repository root:

```text
MSSQL_SA_PASSWORD=<strong-development-password>
```

The `.env` file should remain outside source control.

Build the stack:

```bash
docker compose build
```

Start the stack:

```bash
docker compose up -d
```

Check service status:

```bash
docker compose ps
```

Expected services include:

```text
sql
api
worker
otel-collector
```

The API is exposed on:

```text
http://localhost:5190
```

SQL Server is exposed on:

```text
localhost:1433
```

The OpenTelemetry Collector exposes OTLP on:

```text
4317 - gRPC
4318 - HTTP
```

## Manual Smoke Test

### Register a Webhook Endpoint

```http
POST /api/endpoints
```

Example body:

```json
{
  "url": "http://localhost:5190/api/test-receiver",
  "secret": "test-secret"
}
```

### Submit an Event

```http
POST /api/events
```

Header:

```text
Idempotency-Key: test-delivery-001
```

Example body:

```json
{
  "webhookEndpointId": "WEBHOOK-ENDPOINT-ID",
  "eventType": "test.event",
  "payload": "{\"message\":\"worker test\"}"
}
```

### Check Delivery Status

```http
GET /api/events/{eventId}
```

The response includes the event's current delivery state and persisted attempt history.

## Test Receivers

The API contains development endpoints for exercising delivery behavior.

Successful delivery:

```text
POST /api/test-receiver
```

Forced failure:

```text
POST /api/test-receiver/fail
```

The failure endpoint can be used to verify:

- retries
- backoff behavior
- persisted failures
- dead-letter transitions

## Automated Testing

The project includes automated tests covering reliability behavior such as:

- duplicate idempotency handling
- successful delivery state transitions
- retry exhaustion and dead-letter behavior

SQLite is used for relational tests where database constraints and relational behavior matter.

Run tests with:

```bash
dotnet test
```

## Continuous Integration

GitHub Actions automatically validates the project on pushes and pull requests.

The CI pipeline performs:

```text
restore
→ build
→ automated tests
→ Docker API image build
→ Docker Worker image build
```

This ensures both application code and container definitions remain buildable from a clean environment.

## Health Checks

The API exposes:

```text
GET /health
```

Docker Compose uses health checks and dependency conditions so startup occurs in a controlled sequence:

```text
SQL Server healthy
        ↓
API starts and becomes healthy
        ↓
Worker starts
```

## Development Notes

Configuration is environment-specific.

Application code does not depend on machine-specific hostnames or telemetry destinations.

Runtime configuration such as:

- SQL Server address
- database credentials
- OTLP endpoint

is supplied through configuration files or environment variables depending on the execution environment.

## Future Enhancements

Possible future rounds include:

- production observability backend such as Grafana / Prometheus / Tempo
- authentication and authorization
- message-broker-backed delivery
- additional retry/dead-letter administration APIs
- cloud deployment
- infrastructure as code
- Azure Container Apps
- Terraform or Bicep
- richer integration testing
- operational dashboards and alerts
- distributed tracing across additional downstream services

These are intentionally future enhancements and are not claimed as currently implemented functionality.

## Project Status

Current implementation includes:

- reliable event ingestion
- database persistence
- asynchronous delivery
- idempotency
- retry/backoff/jitter
- dead-letter handling
- delivery history
- automated tests
- Docker Compose deployment
- GitHub Actions CI
- structured logging
- OpenTelemetry metrics and traces
- OTLP export through OpenTelemetry Collector

The project is designed to be extended incrementally in future engineering rounds.