using Microsoft.EntityFrameworkCore;
using ReliableWebhook.Api.Models;

namespace ReliableWebhook.Api.Data;

public class WebhookDbContext : DbContext
{
    public WebhookDbContext(
        DbContextOptions<WebhookDbContext> options)
        : base(options)
    {
    }

    public DbSet<WebhookEndpoint> WebhookEndpoints { get; set; }
}