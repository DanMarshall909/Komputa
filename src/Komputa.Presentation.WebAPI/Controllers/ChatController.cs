using Komputa.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Komputa.Presentation.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly MemoryAwareConversationService _conversationService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        MemoryAwareConversationService conversationService,
        ILogger<ChatController> logger)
    {
        _conversationService = conversationService;
        _logger = logger;
    }

    [HttpPost("message")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            _logger.LogInformation("Received message: {Message}", request.Message);

            var response = await _conversationService.GetResponseWithMemoryAsync(request.Message);

            _logger.LogInformation("Generated response successfully");

            return Ok(new ChatResponse
            {
                Response = response,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            return StatusCode(500, new { error = "Failed to process message", details = ex.Message });
        }
    }

    [HttpGet("memory")]
    public async Task<ActionResult<MemoryStatus>> GetMemoryStatus()
    {
        try
        {
            var status = await _conversationService.GetMemoryStatusAsync();

            return Ok(new MemoryStatus
            {
                Status = status,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving memory status");
            return StatusCode(500, new { error = "Failed to retrieve memory status", details = ex.Message });
        }
    }

    [HttpGet("health")]
    public ActionResult<HealthStatus> GetHealth()
    {
        return Ok(new HealthStatus
        {
            Status = "healthy",
            Service = "Komputa Chat API",
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow
        });
    }
}

public record ChatRequest
{
    public string Message { get; init; } = string.Empty;
}

public record ChatResponse
{
    public string Response { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public record MemoryStatus
{
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public record HealthStatus
{
    public string Status { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
