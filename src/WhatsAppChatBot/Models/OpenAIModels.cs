using System.Text.Json.Serialization;

namespace WhatsAppChatBot.Models;

public class OpenAIChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("tools")]
    public List<Tool>? Tools { get; set; }

    [JsonPropertyName("tool_choice")]
    public string? ToolChoice { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
}

public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public object? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }

    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }
}

public class ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("function")]
    public FunctionCall Function { get; set; } = new();
}

public class FunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

public class Tool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("function")]
    public FunctionDefinition Function { get; set; } = new();
}

public class FunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public object? Parameters { get; set; }

    [JsonPropertyName("strict")]
    public bool? Strict { get; set; }
}

public class OpenAIChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public Usage? Usage { get; set; }
}

public class Choice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public ChatMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class Usage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public class TranscriptionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("response_format")]
    public string ResponseFormat { get; set; } = string.Empty;
}

public class TextToSpeechRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    [JsonPropertyName("voice")]
    public string Voice { get; set; } = string.Empty;

    [JsonPropertyName("speed")]
    public double Speed { get; set; } = 1.0;

    [JsonPropertyName("response_format")]
    public string ResponseFormat { get; set; } = string.Empty;
}

public class ImageAnalysisMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public List<ContentItem> Content { get; set; } = new();
}

public class ContentItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("image_url")]
    public ImageUrl? ImageUrl { get; set; }
}

public class ImageUrl
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
