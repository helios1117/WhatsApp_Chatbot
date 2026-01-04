using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhatsAppChatBot.Common;

namespace WhatsAppChatBot.Bot;

public class ChatBot : IUnifiedAction
{
    public Task<string> PerformAsync()
    {
        var dict = new Dictionary<string, string>
        {
            ["file"] = "src/WhatsAppChatBot/Bot/ChatBot.cs",
            ["class"] = "ChatBot",
            ["result"] = "ok"
        };
        return Task.FromResult(JsonSerializer.Serialize(dict));
    }
}
using System.Text.Json;
using System.Text.RegularExpressions;
using WhatsAppChatBot.Api;
using WhatsAppChatBot.Config;
using WhatsAppChatBot.Models;
using WhatsAppChatBot.Services;

namespace WhatsAppChatBot.Bot;

public interface IChatBot
{
    Task ProcessMessageAsync(MessageData data, WassengerDevice device);
    Task<bool> CanReplyAsync(MessageData data, WassengerDevice device);
    Task AssignChatToAgentAsync(MessageData data, WassengerDevice device, bool force = false);
    Task SendMessageAsync(SendMessageRequest data);
    IWassengerClient GetWassengerClient();
}

public class ChatBot : IChatBot
{
    private readonly BotConfig _config;
    private readonly IOpenAIClient _openAiClient;
    private readonly IWassengerClient _wassengerClient;
    private readonly IFunctionHandler _functionHandler;
    private readonly IMemoryStore _memoryStore;
    private readonly ILogger<ChatBot> _logger;

    public ChatBot(
        BotConfig config,
        IOpenAIClient openAiClient,
        IWassengerClient wassengerClient,
        IFunctionHandler functionHandler,
        IMemoryStore memoryStore,
        ILogger<ChatBot> logger)
    {
        _config = config;
        _openAiClient = openAiClient;
        _wassengerClient = wassengerClient;
        _functionHandler = functionHandler;
        _memoryStore = memoryStore;
        _logger = logger;
    }

    public async Task ProcessMessageAsync(MessageData data, WassengerDevice device)
    {
        try
        {
            if (!await CanReplyAsync(data, device))
            {
                return;
            }

            var chat = data.Chat;
            var chatId = chat.Id;

            if (!HasChatMessagesQuota(chat))
            {
                await UpdateChatOnMessagesQuotaAsync(data, device);
                _logger.LogInformation("Chat quota exceeded: {ChatId}", chatId);
                return;
            }

            if (HasChatMetadataQuotaExceeded(chat))
            {
                _logger.LogInformation("Chat quota previously exceeded: {ChatId}", chatId);
                return;
            }

            _memoryStore.IncrementMessageCount(chatId);

            var body = await ExtractMessageBodyAsync(data);
            _logger.LogInformation("Processing inbound message: chatId={ChatId}, type={Type}, bodyLength={BodyLength}",
                chatId, data.Type, body.Length);

            await _wassengerClient.SendTypingStateAsync(data, device);

            if (Regex.IsMatch(body.Trim(), @"^(human|person|help|stop)$", RegexOptions.IgnoreCase))
            {
                await AssignChatToAgentAsync(data, device);
                return;
            }

            var useAudio = data.Type == "audio";
            await GenerateAndSendResponseAsync(data, device, body, useAudio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message: chatId={ChatId}", data.Chat?.Id ?? "unknown");
        }
    }

    public Task<bool> CanReplyAsync(MessageData data, WassengerDevice device)
    {
        var chat = data.Chat;

        if (chat.Owner?.Agent != null)
        {
            _logger.LogDebug("Skipping chat assigned to agent: {ChatId}", chat.Id);
            return Task.FromResult(false);
        }

        if (chat.FromNumber == device.Phone)
        {
            _logger.LogDebug("Skipping message from same device number");
            return Task.FromResult(false);
        }

        if (chat.Type != "chat")
        {
            _logger.LogDebug("Skipping non-chat message: {Type}", chat.Type);
            return Task.FromResult(false);
        }

        var labels = chat.Labels ?? new List<string>();
        if (_config.SkipChatWithLabels.Any() && labels.Any())
        {
            if (_config.SkipChatWithLabels.Intersect(labels).Any())
            {
                _logger.LogDebug("Skipping chat with blacklisted labels: {Labels}", string.Join(", ", labels));
                return Task.FromResult(false);
            }
        }

        var fromNumber = chat.FromNumber ?? string.Empty;
        if (_config.NumbersWhitelist.Any() && !string.IsNullOrEmpty(fromNumber))
        {
            if (!_config.NumbersWhitelist.Contains(fromNumber))
            {
                _logger.LogDebug("Skipping chat from non-whitelisted number: {FromNumber}", fromNumber);
                return Task.FromResult(false);
            }
        }

        if (_config.NumbersBlacklist.Any() && !string.IsNullOrEmpty(fromNumber))
        {
            if (_config.NumbersBlacklist.Contains(fromNumber))
            {
                _logger.LogDebug("Skipping chat from blacklisted number: {FromNumber}", fromNumber);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    public async Task AssignChatToAgentAsync(MessageData data, WassengerDevice device, bool force = false)
    {
        if (!_config.EnableMemberChatAssignment && !force)
        {
            return;
        }

        try
        {
            var members = await _wassengerClient.PullMembersAsync(device);
            var eligibleMembers = FilterEligibleMembers(members);

            if (!eligibleMembers.Any())
            {
                _logger.LogWarning("No eligible members available for assignment");
                return;
            }

            var random = new Random();
            var selectedMember = eligibleMembers[random.Next(eligibleMembers.Count)];

            await _wassengerClient.AssignChatToAgentAsync(data, device, selectedMember.Id);

            if (_config.SetMetadataOnAssignment.Any())
            {
                var metadata = new List<MetadataItem>();
                foreach (var item in _config.SetMetadataOnAssignment)
                {
                    var value = item.Value == "datetime" ? DateTime.UtcNow.ToString("O") : item.Value;
                    metadata.Add(new MetadataItem { Key = item.Key, Value = value });
                }
                await _wassengerClient.UpdateChatMetadataAsync(data, device, metadata);
            }

            await SendMessageAsync(new SendMessageRequest
            {
                Phone = data.FromNumber,
                Message = _config.TemplateMessages.ChatAssigned,
                Device = device.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign chat to agent");
        }
    }

    public async Task SendMessageAsync(SendMessageRequest data)
    {
        await _wassengerClient.SendMessageAsync(data);
    }

    public IWassengerClient GetWassengerClient()
    {
        return _wassengerClient;
    }

    private List<TeamMember> FilterEligibleMembers(List<TeamMember> members)
    {
        var eligible = new List<TeamMember>();

        foreach (var member in members)
        {
            if (member.Status != "active")
                continue;

            if (_config.SkipTeamRolesFromAssignment.Any() &&
                _config.SkipTeamRolesFromAssignment.Contains(member.Role))
                continue;

            if (_config.TeamWhitelist.Any() &&
                !_config.TeamWhitelist.Contains(member.Id))
                continue;

            if (_config.TeamBlacklist.Any() &&
                _config.TeamBlacklist.Contains(member.Id))
                continue;

            if (_config.AssignOnlyToOnlineMembers &&
                member.Presence != "online")
                continue;

            eligible.Add(member);
        }

        return eligible;
    }

    private bool HasChatMessagesQuota(ChatInfo chat)
    {
        return _memoryStore.HasChatMessagesQuota(
            chat.Id,
            _config.Limits.MaxMessagesPerChat,
            _config.Limits.MaxMessagesPerChatCounterTime);
    }

    private bool HasChatMetadataQuotaExceeded(ChatInfo chat)
    {
        var metadata = chat.Metadata ?? new Dictionary<string, object>();
        return metadata.ContainsKey("bot_quota_exceeded");
    }

    private async Task UpdateChatOnMessagesQuotaAsync(MessageData data, WassengerDevice device)
    {
        try
        {
            var metadata = new List<MetadataItem>
            {
                new() { Key = "bot_quota_exceeded", Value = DateTime.UtcNow.ToString("O") }
            };
            await _wassengerClient.UpdateChatMetadataAsync(data, device, metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chat quota metadata");
        }
    }

    private async Task<string> ExtractMessageBodyAsync(MessageData data)
    {
        var body = data.Body ?? string.Empty;

        if (data.Type == "audio" && !string.IsNullOrEmpty(data.Media?.Id))
        {
            body = await TranscribeAudioAsync(data);
        }

        if (string.IsNullOrEmpty(body))
        {
            body = data.Type switch
            {
                "image" => "User sent an image",
                "video" => "User sent a video",
                "document" => "User sent a document",
                "location" => "User sent a location",
                "contacts" => "User sent contact information",
                _ => "User sent a message"
            };
        }

        var maxLength = Math.Min(_config.Limits.MaxInputCharacters, 10000);
        return body[..Math.Min(body.Length, maxLength)].Trim();
    }

    private async Task<string> TranscribeAudioAsync(MessageData data)
    {
        try
        {
            var mediaId = data.Media?.Id;
            if (string.IsNullOrEmpty(mediaId))
            {
                return string.Empty;
            }

            var audioContent = await _wassengerClient.DownloadMediaAsync(mediaId);
            if (audioContent == null)
            {
                return string.Empty;
            }

            var tempDir = _config.Server.TempPath;
            Directory.CreateDirectory(tempDir);

            var tempFile = Path.Combine(tempDir, $"{Guid.NewGuid()}.mp3");
            await File.WriteAllBytesAsync(tempFile, audioContent);

            var transcription = await _openAiClient.TranscribeAudioAsync(tempFile);
            File.Delete(tempFile);

            return transcription ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transcribe audio");
            return string.Empty;
        }
    }

    private async Task GenerateAndSendResponseAsync(MessageData data, WassengerDevice device, string body, bool useAudio)
    {
        if (string.IsNullOrEmpty(body))
        {
            await SendMessageAsync(new SendMessageRequest
            {
                Phone = data.FromNumber,
                Message = _config.UnknownCommandMessage,
                Device = device.Id
            });
            return;
        }

        var chatId = data.Chat.Id;
        var chatMessages = _memoryStore.GetState(chatId);
        var previousMessages = BuildConversationContext(chatMessages);

        var messages = new List<ChatMessage>
        {
            new() { Role = "system", Content = _config.BotInstructions }
        };

        messages.AddRange(previousMessages);
        messages.Add(new ChatMessage { Role = "user", Content = body });

        StoreMessage(chatId, "user", body);

        var tools = _functionHandler.GetFunctionsForOpenAI();

        try
        {
            var response = await GenerateResponseWithFunctionsAsync(messages, tools, data, device);
            if (string.IsNullOrEmpty(response))
            {
                response = _config.UnknownCommandMessage;
            }

            StoreMessage(chatId, "assistant", response);
            await SendResponseAsync(data, device, response, useAudio);
            await UpdateChatMetadataAsync(data, device);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate response");
            await SendMessageAsync(new SendMessageRequest
            {
                Phone = data.FromNumber,
                Message = _config.UnknownCommandMessage,
                Device = device.Id
            });
        }
    }

    private async Task<string> GenerateResponseWithFunctionsAsync(
        List<ChatMessage> messages,
        List<Tool> tools,
        MessageData data,
        WassengerDevice device)
    {
        const int maxCalls = 5;
        var count = 0;

        while (count < maxCalls)
        {
            count++;
            var response = await _openAiClient.CreateChatCompletionAsync(messages, tools);
            var choice = response.Choices.FirstOrDefault();

            if (choice?.Message == null)
                break;

            var message = choice.Message;
            messages.Add(message);

            if (message.ToolCalls?.Any() == true)
            {
                foreach (var toolCall in message.ToolCalls)
                {
                    if (toolCall.Type == "function")
                    {
                        var functionName = toolCall.Function.Name;
                        var arguments = string.IsNullOrEmpty(toolCall.Function.Arguments)
                            ? new Dictionary<string, object>()
                            : JsonSerializer.Deserialize<Dictionary<string, object>>(toolCall.Function.Arguments) ?? new();

                        var context = new FunctionContext
                        {
                            Data = data,
                            Device = device,
                            Messages = messages
                        };

                        var functionResult = _functionHandler.ExecuteFunction(functionName, arguments, context);

                        messages.Add(new ChatMessage
                        {
                            Role = "tool",
                            ToolCallId = toolCall.Id,
                            Content = functionResult
                        });
                    }
                }
                continue;
            }

            return message.Content?.ToString()?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }

    private List<ChatMessage> BuildConversationContext(Dictionary<string, object> chatMessages)
    {
        var messages = new List<ChatMessage>();
        var limit = _config.Limits.ChatHistoryLimit;

        var messageList = new List<ConversationMessage>();
        foreach (var kvp in chatMessages)
        {
            if (kvp.Value is JsonElement element)
            {
                try
                {
                    var msg = JsonSerializer.Deserialize<ConversationMessage>(element.GetRawText());
                    if (msg != null)
                    {
                        messageList.Add(msg);
                    }
                }
                catch
                {
                    // Skip invalid messages
                }
            }
        }

        // Sort by date, limit, and reverse
        messageList = messageList
            .OrderByDescending(m => m.Date)
            .Take(limit)
            .OrderBy(m => m.Date)
            .ToList();

        foreach (var msg in messageList)
        {
            if (!string.IsNullOrEmpty(msg.Content))
            {
                messages.Add(new ChatMessage { Role = msg.Role, Content = msg.Content });
            }
        }

        return messages;
    }

    private void StoreMessage(string chatId, string role, string content)
    {
        var messages = _memoryStore.GetState(chatId);
        var messageId = Guid.NewGuid().ToString();

        var message = new ConversationMessage
        {
            Role = role,
            Content = content,
            Date = DateTime.UtcNow
        };

        messages[messageId] = message;
        _memoryStore.SetState(chatId, messages);
    }

    private async Task SendResponseAsync(MessageData data, WassengerDevice device, string response, bool useAudio)
    {
        var messageData = new SendMessageRequest
        {
            Phone = data.FromNumber,
            Device = device.Id
        };

        if (useAudio && _config.Features.AudioOutput)
        {
            var audioContent = await _openAiClient.GenerateSpeechAsync(
                response,
                _config.Features.Voice,
                _config.Features.VoiceSpeed);

            if (audioContent != null)
            {
                var tempDir = _config.Server.TempPath;
                Directory.CreateDirectory(tempDir);

                var audioFile = Path.Combine(tempDir, $"{Guid.NewGuid()}.mp3");
                await File.WriteAllBytesAsync(audioFile, audioContent);

                messageData.Media = audioFile;
                messageData.Message = string.Empty;
            }
            else
            {
                messageData.Message = response;
            }
        }
        else
        {
            messageData.Message = response;
        }

        await SendMessageAsync(messageData);
    }

    private async Task UpdateChatMetadataAsync(MessageData data, WassengerDevice device)
    {
        try
        {
            if (_config.SetLabelsOnBotChats.Any())
            {
                await _wassengerClient.UpdateChatLabelsAsync(data, device, _config.SetLabelsOnBotChats);
            }

            if (_config.SetMetadataOnBotChats.Any())
            {
                var metadata = new List<MetadataItem>();
                foreach (var item in _config.SetMetadataOnBotChats)
                {
                    var value = item.Value == "datetime" ? DateTime.UtcNow.ToString("O") : item.Value;
                    metadata.Add(new MetadataItem { Key = item.Key, Value = value });
                }
                await _wassengerClient.UpdateChatMetadataAsync(data, device, metadata);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chat metadata");
        }
    }
}
