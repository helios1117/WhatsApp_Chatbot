using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WhatsAppChatBot.Config;
using WhatsAppChatBot.Models;
using WhatsAppChatBot.Services;

namespace WhatsAppChatBot.Api;

public interface IWassengerClient
{
    Task<ApiResponse<object>> SendMessageAsync(SendMessageRequest data);
    Task<WassengerDevice?> LoadDeviceAsync(string? deviceId = null);
    Task<List<TeamMember>> PullMembersAsync(WassengerDevice device);
    Task<List<WassengerLabel>> PullLabelsAsync(WassengerDevice device, bool force = false);
    Task CreateLabelsAsync(WassengerDevice device, List<string> requiredLabels);
    Task UpdateChatLabelsAsync(MessageData data, WassengerDevice device, List<string> labels);
    Task UpdateChatMetadataAsync(MessageData data, WassengerDevice device, List<MetadataItem> metadata);
    Task AssignChatToAgentAsync(MessageData data, WassengerDevice device, string agentId);
    Task<WebhookRegistrationResponse?> RegisterWebhookAsync(string webhookUrl, WassengerDevice device);
    Task SendTypingStateAsync(MessageData data, WassengerDevice device, string action = "typing");
    Task<byte[]?> DownloadMediaAsync(string mediaId);
}

public class WassengerClient : IWassengerClient
{
    private readonly HttpClient _httpClient;
    private readonly BotConfig _config;
    private readonly IMemoryStore _memoryStore;
    private readonly ILogger<WassengerClient> _logger;

    public WassengerClient(
        HttpClient httpClient,
        BotConfig config,
        IMemoryStore memoryStore,
        ILogger<WassengerClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _memoryStore = memoryStore;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", config.Api.ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.BaseAddress = new Uri(config.Api.ApiBaseUrl);
    }

    public async Task<ApiResponse<object>> SendMessageAsync(SendMessageRequest data)
    {
        const int maxRetries = 3;
        var retries = maxRetries;

        while (retries > 0)
        {
            retries--;
            try
            {
                var requestData = new
                {
                    phone = data.Phone,
                    message = data.Message,
                    device = data.Device,
                    media = data.Media,
                    reference = data.Reference,
                    enqueue = "never"
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/messages", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<object>(responseJson);

                _logger.LogDebug("Message sent successfully");
                return new ApiResponse<object> { Success = true, Data = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message, retries left: {Retries}", retries);
                if (retries == 0)
                {
                    throw;
                }
                await Task.Delay(1000);
            }
        }

        return new ApiResponse<object> { Success = false, Error = "Failed to send message after retries" };
    }

    public async Task<WassengerDevice?> LoadDeviceAsync(string? deviceId = null)
    {
        try
        {
            var cacheKey = $"device:{deviceId ?? "default"}";
            var cachedDevice = _memoryStore.GetCache<WassengerDevice>(cacheKey);
            if (cachedDevice != null)
            {
                return cachedDevice;
            }

            var url = string.IsNullOrEmpty(deviceId) ? "/devices" : $"/devices/{deviceId}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(deviceId))
            {
                var devices = JsonSerializer.Deserialize<List<WassengerDevice>>(responseJson);
                var device = devices?.FirstOrDefault();
                if (device != null)
                {
                    _memoryStore.SetCache(cacheKey, device, TimeSpan.FromMinutes(5));
                }
                return device;
            }
            else
            {
                var device = JsonSerializer.Deserialize<WassengerDevice>(responseJson);
                if (device != null)
                {
                    _memoryStore.SetCache(cacheKey, device, TimeSpan.FromMinutes(5));
                }
                return device;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load device: {DeviceId}", deviceId);
            return null;
        }
    }

    public async Task<List<TeamMember>> PullMembersAsync(WassengerDevice device)
    {
        try
        {
            var cacheKey = $"members:{device.Id}";
            var cachedMembers = _memoryStore.GetCache<List<TeamMember>>(cacheKey);
            if (cachedMembers != null)
            {
                return cachedMembers;
            }

            var response = await _httpClient.GetAsync($"/chat/{device.Id}/members");
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var members = JsonSerializer.Deserialize<List<TeamMember>>(responseJson) ?? new List<TeamMember>();

            _memoryStore.SetCache(cacheKey, members, TimeSpan.FromMinutes(10));
            return members;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pull members for device: {DeviceId}", device.Id);
            return new List<TeamMember>();
        }
    }

    public async Task<List<WassengerLabel>> PullLabelsAsync(WassengerDevice device, bool force = false)
    {
        try
        {
            var cacheKey = $"labels:{device.Id}";
            if (!force)
            {
                var cachedLabels = _memoryStore.GetCache<List<WassengerLabel>>(cacheKey);
                if (cachedLabels != null)
                {
                    return cachedLabels;
                }
            }

            var response = await _httpClient.GetAsync($"/chat/{device.Id}/labels");
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var labels = JsonSerializer.Deserialize<List<WassengerLabel>>(responseJson) ?? new List<WassengerLabel>();

            _memoryStore.SetCache(cacheKey, labels, TimeSpan.FromMinutes(10));
            return labels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pull labels for device: {DeviceId}", device.Id);
            return new List<WassengerLabel>();
        }
    }

    public async Task CreateLabelsAsync(WassengerDevice device, List<string> requiredLabels)
    {
        try
        {
            var existingLabels = await PullLabelsAsync(device);
            var existingLabelNames = existingLabels.Select(l => l.Name.ToLower()).ToHashSet();

            foreach (var labelName in requiredLabels)
            {
                if (!existingLabelNames.Contains(labelName.ToLower()))
                {
                    var createRequest = new CreateLabelRequest
                    {
                        Name = labelName,
                        Color = "#007bff"
                    };

                    var json = JsonSerializer.Serialize(createRequest);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync($"/chat/{device.Id}/labels", content);
                    response.EnsureSuccessStatusCode();

                    _logger.LogDebug("Created label: {LabelName}", labelName);
                }
            }

            // Refresh cache
            await PullLabelsAsync(device, force: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create labels for device: {DeviceId}", device.Id);
        }
    }

    public async Task UpdateChatLabelsAsync(MessageData data, WassengerDevice device, List<string> labels)
    {
        try
        {
            var chatId = data.Chat.Id;
            var request = new UpdateChatLabelsRequest { Labels = labels };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PatchAsync($"/chat/{device.Id}/chats/{chatId}/labels", content);
            response.EnsureSuccessStatusCode();

            _logger.LogDebug("Updated chat labels: {ChatId}, labels: {Labels}", chatId, string.Join(", ", labels));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chat labels: {ChatId}", data.Chat.Id);
        }
    }

    public async Task UpdateChatMetadataAsync(MessageData data, WassengerDevice device, List<MetadataItem> metadata)
    {
        try
        {
            var chatId = data.Chat.Id;
            var request = new UpdateChatMetadataRequest { Metadata = metadata };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PatchAsync($"/chat/{device.Id}/chats/{chatId}/metadata", content);
            response.EnsureSuccessStatusCode();

            _logger.LogDebug("Updated chat metadata: {ChatId}", chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chat metadata: {ChatId}", data.Chat.Id);
        }
    }

    public async Task AssignChatToAgentAsync(MessageData data, WassengerDevice device, string agentId)
    {
        try
        {
            var chatId = data.Chat.Id;
            var request = new AssignChatRequest { Agent = agentId };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PatchAsync($"/chat/{device.Id}/chats/{chatId}/owner", content);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Assigned chat {ChatId} to agent {AgentId}", chatId, agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign chat to agent: {ChatId}", data.Chat.Id);
            throw;
        }
    }

    public async Task<WebhookRegistrationResponse?> RegisterWebhookAsync(string webhookUrl, WassengerDevice device)
    {
        try
        {
            await Task.Delay(1000); // Wait a bit before registering

            var request = new WebhookRegistrationRequest
            {
                Url = webhookUrl,
                Name = "Chatbot",
                Events = new List<string> { "message:in:new" },
                Device = device.Id
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/webhooks", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var webhook = JsonSerializer.Deserialize<WebhookRegistrationResponse>(responseJson);

            _logger.LogInformation("Webhook registered successfully: {Url}", webhookUrl);
            return webhook;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register webhook: {Url}", webhookUrl);
            throw;
        }
    }

    public async Task SendTypingStateAsync(MessageData data, WassengerDevice device, string action = "typing")
    {
        try
        {
            var request = new TypingRequest
            {
                Action = action,
                Duration = 10,
                Chat = data.FromNumber
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/chat/{device.Id}/typing", content);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to send typing state");
        }
    }

    public async Task<byte[]?> DownloadMediaAsync(string mediaId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/media/{mediaId}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download media: {MediaId}", mediaId);
            return null;
        }
    }
}
