using Microsoft.AspNetCore.Mvc;
using WhatsAppChatBot.Bot;
using WhatsAppChatBot.Config;
using WhatsAppChatBot.Models;

namespace WhatsAppChatBot.Controllers;

[ApiController]
[Route("/")]
public class WebhookController : ControllerBase
{
    private readonly IChatBot _chatBot;
    private readonly BotConfig _config;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IChatBot chatBot, BotConfig config, ILogger<WebhookController> logger)
    {
        _chatBot = chatBot;
        _config = config;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return Ok(new
        {
            name = "chatbot",
            description = "WhatsApp ChatGPT-powered Chatbot for Wassenger",
            version = "1.0.0",
            endpoints = new
            {
                webhook = new { path = "/webhook", method = "POST" },
                sendMessage = new { path = "/message", method = "POST" },
                sample = new { path = "/sample", method = "GET" }
            }
        });
    }

    [HttpPost("webhook")]
    public Task<IActionResult> Webhook([FromBody] WebhookMessage body)
    {
        try
        {
            _logger.LogDebug("Received webhook: {Event}", body.Event);

            if (body.Event == "message:in:new")
            {
                // Process message in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var device = await LoadDeviceAsync();
                        if (device != null)
                        {
                            await _chatBot.ProcessMessageAsync(body.Data, device);
                        }
                        else
                        {
                            _logger.LogError("No active device found for message processing");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process inbound message");
                    }
                });
            }

            return Task.FromResult<IActionResult>(Ok(new { success = true }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return Task.FromResult<IActionResult>(StatusCode(500, new { error = "Internal server error" }));
        }
    }

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            await _chatBot.SendMessageAsync(request);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message");
            return StatusCode(500, new { error = "Failed to send message" });
        }
    }

    [HttpGet("sample")]
    public async Task<IActionResult> Sample()
    {
        try
        {
            var device = await LoadDeviceAsync();
            if (device == null)
            {
                return BadRequest(new { error = "No active device found" });
            }

            // This would send a sample message to a test number
            // Implementation depends on your testing needs
            return Ok(new {
                message = "Sample endpoint - implement according to your testing needs",
                device = device.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send sample message");
            return StatusCode(500, new { error = "Failed to send sample message" });
        }
    }

    [HttpGet("files/{fileId}")]
    public async Task<IActionResult> FileDownload(string fileId)
    {
        try
        {
            var tempPath = _config.Server.TempPath;
            var filePath = Path.Combine(tempPath, fileId);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "File not found" });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);
            var contentType = GetContentType(fileName);

            // Clean up file after serving
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                try
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up temporary file: {FilePath}", filePath);
                }
            });

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file: {FileId}", fileId);
            return StatusCode(500, new { error = "Failed to download file" });
        }
    }

    private async Task<WassengerDevice?> LoadDeviceAsync()
    {
        try
        {
            return await _chatBot.GetWassengerClient().LoadDeviceAsync(_config.Server.Device);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load device");
            return null;
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".m4a" => "audio/mp4",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
