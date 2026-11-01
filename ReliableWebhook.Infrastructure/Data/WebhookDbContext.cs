using Microsoft.EntityFrameworkCore;
using ReliableWebhook.Infrastructure.Models;

namespace ReliableWebhook.Infrastructure.Data;

public class WebhookDbContext : DbContext
{
    public WebhookDbContext(
        DbContextOptions<WebhookDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WebhookEvent>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique();
    }
    public DbSet<WebhookDeliveryAttempt> WebhookDeliveryAttempts { get; set; }

    public DbSet<WebhookEvent> WebhookEvents { get; set; }
    public DbSet<WebhookEndpoint> WebhookEndpoints { get; set; }
}