using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhatsAppChatBot.Common;

namespace WhatsAppChatBot.Models;

public class WassengerModels : IUnifiedAction
{
    public Task<string> PerformAsync()
    {
        var dict = new Dictionary<string, string>
        {
            ["file"] = "src/WhatsAppChatBot/Models/WassengerModels.cs",
            ["class"] = "WassengerModels",
            ["result"] = "ok"
        };
        return Task.FromResult(JsonSerializer.Serialize(dict));
    }
}
using System.Text.Json.Serialization;

namespace WhatsAppChatBot.Models;

public class WassengerDevice
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("alias")]
    public string? Alias { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("session")]
    public DeviceSession? Session { get; set; }

    [JsonPropertyName("billing")]
    public DeviceBilling? Billing { get; set; }
}

public class DeviceSession
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class DeviceBilling
{
    [JsonPropertyName("subscription")]
    public Subscription? Subscription { get; set; }
}

public class Subscription
{
    [JsonPropertyName("product")]
    public string Product { get; set; } = string.Empty;
}

public class TeamMember
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("presence")]
    public string Presence { get; set; } = string.Empty;
}

public class WassengerLabel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
}

public class CreateLabelRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
}

public class UpdateChatLabelsRequest
{
    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; } = new();
}

public class UpdateChatMetadataRequest
{
    [JsonPropertyName("metadata")]
    public List<MetadataItem> Metadata { get; set; } = new();
}

public class MetadataItem
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class AssignChatRequest
{
    [JsonPropertyName("agent")]
    public string Agent { get; set; } = string.Empty;
}

public class WebhookRegistrationRequest
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = new();

    [JsonPropertyName("device")]
    public string Device { get; set; } = string.Empty;
}

public class WebhookRegistrationResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = new();
}

public class TypingRequest
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("chat")]
    public string Chat { get; set; } = string.Empty;
}
