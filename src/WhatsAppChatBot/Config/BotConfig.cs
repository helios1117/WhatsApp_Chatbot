using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhatsAppChatBot.Common;

namespace WhatsAppChatBot.Config;

public class BotConfig : IUnifiedAction
{
    public Task<string> PerformAsync()
    {
        var dict = new Dictionary<string, string>
        {
            ["file"] = "src/WhatsAppChatBot/Config/BotConfig.cs",
            ["class"] = "BotConfig",
            ["result"] = "ok"
        };
        return Task.FromResult(JsonSerializer.Serialize(dict));
    }
}
using Microsoft.Extensions.Options;

namespace WhatsAppChatBot.Config;

public class BotConfig
{
    public ApiConfig Api { get; set; } = new();
    public ServerConfig Server { get; set; } = new();
    public FeaturesConfig Features { get; set; } = new();
    public LimitsConfig Limits { get; set; } = new();
    public TemplateMessagesConfig TemplateMessages { get; set; } = new();
    public InferenceParamsConfig InferenceParams { get; set; } = new();
    public int CacheTtl { get; set; } = 600; // 10 minutes
    public string BotInstructions { get; set; } = DefaultBotInstructions;
    public string WelcomeMessage { get; set; } = DefaultWelcomeMessage;
    public string DefaultMessage { get; set; } = DefaultDefaultMessage;
    public string UnknownCommandMessage { get; set; } = DefaultUnknownCommandMessage;
    public List<string> SetLabelsOnBotChats { get; set; } = new() { "bot" };
    public bool RemoveLabelsAfterAssignment { get; set; } = true;
    public List<string> SetLabelsOnUserAssignment { get; set; } = new() { "from-bot" };
    public List<string> SkipChatWithLabels { get; set; } = new() { "no-bot" };
    public List<string> NumbersBlacklist { get; set; } = new();
    public List<string> NumbersWhitelist { get; set; } = new();
    public List<string> TeamBlacklist { get; set; } = new();
    public List<string> TeamWhitelist { get; set; } = new();
    public List<string> SkipTeamRolesFromAssignment { get; set; } = new() { "admin", "owner" };
    public bool SkipArchivedChats { get; set; } = true;
    public bool EnableMemberChatAssignment { get; set; } = true;
    public bool AssignOnlyToOnlineMembers { get; set; } = false;
    public List<MetadataConfig> SetMetadataOnBotChats { get; set; } = new();
    public List<MetadataConfig> SetMetadataOnAssignment { get; set; } = new();

    private const string DefaultWelcomeMessage =
        "Hey there 👋 Welcome to this ChatGPT-powered AI chatbot demo using *Wassenger API*! I can also speak many languages 😁";

    private const string DefaultDefaultMessage =
        "Don't be shy 😁 try asking anything to the AI chatbot, using natural language!\n\n" +
        "Example queries:\n\n" +
        "1️⃣ Explain me what is Wassenger\n" +
        "2️⃣ Can I use Wassenger to send automatic messages?\n" +
        "3️⃣ Can I schedule messages using Wassenger?\n" +
        "4️⃣ Is there a free trial available?\n\n" +
        "Type *human* to talk with a person. The chat will be assigned to an available member of the team.\n\n" +
        "Give it a try! 😁";

    private const string DefaultUnknownCommandMessage =
        "I'm sorry, I was unable to understand your message. Can you please elaborate more?\n\n" +
        "If you would like to chat with a human, just reply with *human*.";

    private const string DefaultBotInstructions =
        "You are a smart virtual customer support assistant who works for Wassenger.\n" +
        "You can identify yourself as Milo, the Wassenger AI Assistant.\n" +
        "You will be chatting with random customers who may contact you with general queries about the product.\n" +
        "Wassenger is a cloud solution that offers WhatsApp API and multi-user live communication services designed for businesses and developers.\n" +
        "Wassenger also enables customers to automate WhatsApp communication and build chatbots.\n" +
        "You are an expert customer support agent.\n" +
        "Be polite. Be helpful. Be emphatic. Be concise.\n" +
        "Politely reject any queries that are not related to customer support tasks or Wassenger services itself.\n" +
        "Stick strictly to your role as a customer support virtual assistant for Wassenger.\n" +
        "Always speak in the language the user prefers or uses.\n" +
        "If you can't help with something, ask the user to type *human* in order to talk with customer support.\n" +
        "Do not use Markdown formatted and rich text, only raw text.";
}

public class ApiConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api.wassenger.com/v1";
    public string OpenAiKey { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = "gpt-4o";
}

public class ServerConfig
{
    public int Port { get; set; } = 8080;
    public string TempPath { get; set; } = ".tmp";
    public string? Device { get; set; }
    public string? WebhookUrl { get; set; }
    public string? NgrokToken { get; set; }
    public bool Production { get; set; } = false;
}

public class FeaturesConfig
{
    public bool AudioInput { get; set; } = true;
    public bool AudioOutput { get; set; } = true;
    public bool AudioOnly { get; set; } = false;
    public string Voice { get; set; } = "echo";
    public double VoiceSpeed { get; set; } = 1.0;
    public bool ImageInput { get; set; } = true;
}

public class LimitsConfig
{
    public int MaxInputCharacters { get; set; } = 1000;
    public int MaxOutputTokens { get; set; } = 1000;
    public int ChatHistoryLimit { get; set; } = 20;
    public int MaxMessagesPerChat { get; set; } = 500;
    public int MaxMessagesPerChatCounterTime { get; set; } = 24 * 60 * 60; // 24 hours
    public int MaxAudioDuration { get; set; } = 2 * 60; // 2 minutes
    public int MaxImageSize { get; set; } = 2 * 1024 * 1024; // 2MB
}

public class TemplateMessagesConfig
{
    public string NoAudioAccepted { get; set; } = "Audio messages are not supported: gently ask the user to send text messages only.";
    public string ChatAssigned { get; set; } = "You will be contact shortly by someone from our team. Thank you for your patience.";
}

public class InferenceParamsConfig
{
    public double Temperature { get; set; } = 0.2;
}

public class MetadataConfig
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public static class BotConfigExtensions
{
    public static BotConfig LoadFromEnvironment()
    {
        return new BotConfig
        {
            Api = new ApiConfig
            {
                ApiKey = Environment.GetEnvironmentVariable("API_KEY") ?? string.Empty,
                ApiBaseUrl = Environment.GetEnvironmentVariable("API_URL") ?? "https://api.wassenger.com/v1",
                OpenAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty,
                OpenAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o"
            },
            Server = new ServerConfig
            {
                Port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out int port) ? port : 8080,
                TempPath = Environment.GetEnvironmentVariable("TEMP_PATH") ?? ".tmp",
                Device = Environment.GetEnvironmentVariable("DEVICE"),
                WebhookUrl = Environment.GetEnvironmentVariable("WEBHOOK_URL"),
                NgrokToken = Environment.GetEnvironmentVariable("NGROK_TOKEN"),
                Production = Environment.GetEnvironmentVariable("PRODUCTION")?.ToLower() == "true"
            }
        };
    }

    public static void ValidateConfig(this BotConfig config)
    {
        if (string.IsNullOrEmpty(config.Api.ApiKey) || config.Api.ApiKey.Length < 60)
        {
            throw new InvalidOperationException("Please sign up in Wassenger and obtain your API key: https://app.wassenger.com/apikeys");
        }

        if (string.IsNullOrEmpty(config.Api.OpenAiKey) || config.Api.OpenAiKey.Length < 45)
        {
            throw new InvalidOperationException("Missing required OpenAI API key: please sign up for free and obtain your API key: https://platform.openai.com/account/api-keys");
        }
    }
}
