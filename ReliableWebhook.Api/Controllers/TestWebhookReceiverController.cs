using Microsoft.AspNetCore.Mvc;

namespace ReliableWebhook.Api.Controllers;

[ApiController]
[Route("api/test-receiver")]
public class TestWebhookReceiverController : ControllerBase
{
    private readonly ILogger<TestWebhookReceiverController> _logger;

    public TestWebhookReceiverController(
        ILogger<TestWebhookReceiverController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Receive([FromBody] object payload)
    {
        Console.WriteLine("CALLBACK RECEVIER... I GOT THE MESSAGE");

        _logger.LogInformation(
            "WEBHOOK RECEIVED successfully: {Payload}",
            payload);

        return Ok();
    }

    [HttpPost("fail")]
    public IActionResult Fail([FromBody] object payload)
    {
        Console.WriteLine("CALLBACK RECEVIER... I GOT THE MESSAGE but fail.");
        _logger.LogWarning(
            "WEBHOOK FAILURE TEST returning HTTP 500: {Payload}",
            payload);

        return StatusCode(500);
    }
}