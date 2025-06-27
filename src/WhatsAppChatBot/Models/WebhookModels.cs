using System.Text.Json.Serialization;

namespace WhatsAppChatBot.Models;

public class WebhookMessage
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public MessageData Data { get; set; } = new();
}

public class MessageData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("fromNumber")]
    public string FromNumber { get; set; } = string.Empty;

    [JsonPropertyName("chat")]
    public ChatInfo Chat { get; set; } = new();

    [JsonPropertyName("media")]
    public MediaInfo? Media { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

public class ChatInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("fromNumber")]
    public string FromNumber { get; set; } = string.Empty;

    [JsonPropertyName("labels")]
    public List<string>? Labels { get; set; }

    [JsonPropertyName("owner")]
    public ChatOwner? Owner { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ChatOwner
{
    [JsonPropertyName("agent")]
    public string? Agent { get; set; }
}

public class MediaInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("mimetype")]
    public string MimeType { get; set; } = string.Empty;

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class SendMessageRequest
{
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("device")]
    public string Device { get; set; } = string.Empty;

    [JsonPropertyName("media")]
    public string? Media { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }
}

public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
