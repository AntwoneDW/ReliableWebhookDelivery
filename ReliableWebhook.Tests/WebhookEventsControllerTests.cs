using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using ReliableWebhook.Api.Controllers;
using ReliableWebhook.Api.Dtos;
using ReliableWebhook.Infrastructure.Data;
using ReliableWebhook.Infrastructure.Models;
using Xunit;

namespace ReliableWebhook.Tests;

public class WebhookEventsControllerTests
{
    [Fact]
    public async Task Create_SameIdempotencyKey_DoesNotCreateDuplicateEvent()
    {
        // Arrange
        using var connection =
            new SqliteConnection("DataSource=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<WebhookDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var db =
            new WebhookDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var endpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com/webhook",
            Secret = "test-secret",
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        db.WebhookEndpoints.Add(endpoint);
        await db.SaveChangesAsync();

        var controller =
            new WebhookEventsController(db);

        var request = new CreateWebhookEventRequest
        {
            WebhookEndpointId = endpoint.Id,
            EventType = "order.paid",
            Payload = "{\"orderId\":938}"
        };

        const string key = "order-938-paid-v1";

        // Act
        var first =
            await controller.Create(request, key);

        var second =
            await controller.Create(request, key);

        // Assert
        var count =
            await db.WebhookEvents.CountAsync();

        Assert.Equal(1, count);

        var firstResult =
            Assert.IsType<CreatedResult>(
                first.Result);

        var secondResult =
            Assert.IsType<OkObjectResult>(
                second.Result);

        var firstResponse =
            Assert.IsType<WebhookEventResponse>(
                firstResult.Value);

        var secondResponse =
            Assert.IsType<WebhookEventResponse>(
                secondResult.Value);

        Assert.Equal(
            firstResponse.Id,
            secondResponse.Id);
    }
}