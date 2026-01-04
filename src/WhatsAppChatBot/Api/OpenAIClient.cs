using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhatsAppChatBot.Common;

namespace WhatsAppChatBot.Api;

public class OpenAIClient : IUnifiedAction
{
    public Task<string> PerformAsync()
    {
        var dict = new Dictionary<string, string>
        {
            ["file"] = "src/WhatsAppChatBot/Api/OpenAIClient.cs",
            ["class"] = "OpenAIClient",
            ["result"] = "ok"
        };
        return Task.FromResult(JsonSerializer.Serialize(dict));
    }
}
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WhatsAppChatBot.Models;
using WhatsAppChatBot.Config;

namespace WhatsAppChatBot.Api;

public interface IOpenAIClient
{
    Task<OpenAIChatCompletionResponse> CreateChatCompletionAsync(
        List<ChatMessage> messages,
        List<Tool>? tools = null,
        Dictionary<string, object>? parameters = null);

    Task<string?> TranscribeAudioAsync(string audioFilePath);
    Task<byte[]?> GenerateSpeechAsync(string text, string voice = "echo", double speed = 1.0);
    Task<string?> AnalyzeImageAsync(string imageUrl, string prompt = "Describe this image");
}

public class OpenAIClient : IOpenAIClient
{
    private readonly HttpClient _httpClient;
    private readonly BotConfig _config;
    private readonly ILogger<OpenAIClient> _logger;
    private readonly string _baseUrl = "https://api.openai.com/v1";

    public OpenAIClient(HttpClient httpClient, BotConfig config, ILogger<OpenAIClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", config.Api.OpenAiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<OpenAIChatCompletionResponse> CreateChatCompletionAsync(
        List<ChatMessage> messages,
        List<Tool>? tools = null,
        Dictionary<string, object>? parameters = null)
    {
        var requestData = new OpenAIChatCompletionRequest
        {
            Model = _config.Api.OpenAiModel,
            Messages = messages,
            MaxTokens = _config.Limits.MaxOutputTokens,
            Temperature = _config.InferenceParams.Temperature
        };

        if (tools?.Any() == true)
        {
            requestData.Tools = tools;
            requestData.ToolChoice = "auto";
        }

        // Apply additional parameters
        if (parameters != null)
        {
            // This would require a more complex implementation to merge parameters
            // For now, we'll use the defaults
        }

        try
        {
            _logger.LogDebug("Creating chat completion: model={Model}, messages_count={MessageCount}, tools_count={ToolCount}",
                _config.Api.OpenAiModel, messages.Count, tools?.Count ?? 0);

            var json = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OpenAIChatCompletionResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize OpenAI response");
            }

            _logger.LogDebug("Chat completion created: usage={Usage}, finish_reason={FinishReason}",
                result.Usage?.TotalTokens, result.Choices.FirstOrDefault()?.FinishReason);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create chat completion: model={Model}", _config.Api.OpenAiModel);
            throw;
        }
    }

    public async Task<string?> TranscribeAudioAsync(string audioFilePath)
    {
        try
        {
            _logger.LogDebug("Transcribing audio: {FilePath}", audioFilePath);

            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(audioFilePath);
            using var fileContent = new StreamContent(fileStream);

            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
            form.Add(fileContent, "file", Path.GetFileName(audioFilePath));
            form.Add(new StringContent("whisper-1"), "model");
            form.Add(new StringContent("text"), "response_format");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/transcriptions", form);
            response.EnsureSuccessStatusCode();

            var transcription = (await response.Content.ReadAsStringAsync()).Trim();

            _logger.LogDebug("Audio transcribed successfully, length={Length}", transcription.Length);
            return string.IsNullOrEmpty(transcription) ? null : transcription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transcribe audio: file={FilePath}", audioFilePath);
            return null;
        }
    }

    public async Task<byte[]?> GenerateSpeechAsync(string text, string voice = "echo", double speed = 1.0)
    {
        try
        {
            _logger.LogDebug("Generating speech: text_length={Length}, voice={Voice}, speed={Speed}",
                text.Length, voice, speed);

            var requestData = new TextToSpeechRequest
            {
                Model = "tts-1",
                Input = text,
                Voice = voice,
                Speed = speed,
                ResponseFormat = "mp3"
            };

            var json = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/speech", content);

            response.EnsureSuccessStatusCode();

            var audioContent = await response.Content.ReadAsByteArrayAsync();

            _logger.LogDebug("Speech generated successfully, size={Size}", audioContent.Length);
            return audioContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate speech: text_length={Length}", text.Length);
            return null;
        }
    }

    public async Task<string?> AnalyzeImageAsync(string imageUrl, string prompt = "Describe this image")
    {
        try
        {
            _logger.LogDebug("Analyzing image: url={Url}", imageUrl);

            var requestData = new OpenAIChatCompletionRequest
            {
                Model = "gpt-4o",
                Messages = new List<ChatMessage>
                {
                    new()
                    {
                        Role = "user",
                        Content = new List<ContentItem>
                        {
                            new() { Type = "text", Text = prompt },
                            new() { Type = "image_url", ImageUrl = new ImageUrl { Url = imageUrl } }
                        }
                    }
                },
                MaxTokens = 300
            };

            var json = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OpenAIChatCompletionResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var description = result?.Choices.FirstOrDefault()?.Message.Content?.ToString()?.Trim();

            _logger.LogDebug("Image analyzed successfully, description_length={Length}", description?.Length ?? 0);
            return string.IsNullOrEmpty(description) ? null : description;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze image: url={Url}", imageUrl);
            return null;
        }
    }
}
