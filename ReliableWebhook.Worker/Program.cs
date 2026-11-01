using Microsoft.EntityFrameworkCore;
using ReliableWebhook.Infrastructure.Data;
using ReliableWebhook.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<WebhookDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("WebhookDb")));

builder.Services.AddHttpClient();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

Console.WriteLine(
    $"Environment: {builder.Environment.EnvironmentName}");

Console.WriteLine(
    $"Connection string exists: " +
    $"{builder.Configuration.GetConnectionString("WebhookDb") is not null}");


var connectionString =
    builder.Configuration.GetConnectionString("WebhookDb")
    ?? throw new InvalidOperationException(
        "Connection string 'WebhookDb' was not configured."); 
host.Run();