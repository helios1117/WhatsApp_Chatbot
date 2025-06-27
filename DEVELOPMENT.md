# Development Guide - WhatsApp ChatGPT Bot C#

This guide provides detailed information for developers working on the WhatsApp ChatGPT Bot C# implementation.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Key Components](#key-components)
- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Configuration Management](#configuration-management)
- [API Integration](#api-integration)
- [Testing](#testing)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)

## Architecture Overview

The C# implementation follows modern .NET 8 patterns with clean architecture principles:

```
┌─────────────────────────────────────────────────────────────┐
│                    HTTP Layer (ASP.NET Core)                │
├─────────────────────────────────────────────────────────────┤
│  Controllers/WebhookController.cs - REST API endpoints      │
├─────────────────────────────────────────────────────────────┤
│                    Business Logic Layer                     │
├─────────────────────────────────────────────────────────────┤
│  Bot/ChatBot.cs - Core message processing                   │
│  Bot/FunctionHandler.cs - OpenAI function calling           │
├─────────────────────────────────────────────────────────────┤
│                    Service Layer                            │
├─────────────────────────────────────────────────────────────┤
│  Api/OpenAIClient.cs - OpenAI API integration               │
│  Api/WassengerClient.cs - WhatsApp API integration          │
│  Services/MemoryStore.cs - Caching and state management     │
│  Services/NgrokTunnel.cs - Development tunneling            │
├─────────────────────────────────────────────────────────────┤
│                    Configuration Layer                      │
├─────────────────────────────────────────────────────────────┤
│  Config/BotConfig.cs - Centralized configuration            │
│  Models/ - Data transfer objects                            │
└─────────────────────────────────────────────────────────────┘
```

## Key Components

### Main Application (`Program.cs`)

The main application handles:
- Dependency injection configuration
- Service registration and lifetime management
- Environment-based configuration loading
- Application startup and initialization
- Development vs production mode handling

**Key Features:**
- `LoadFromEnvironment()` - Loads configuration from environment variables
- `InitializeBotServicesAsync()` - Complete bot initialization
- `SetupWebhookAsync()` - Webhook registration with Ngrok support
- HTTP client configuration with retry policies

### ChatBot (`Bot/ChatBot.cs`)

Core bot functionality:
- Message processing and filtering
- Chat assignment and human handoff
- Rate limiting and quota management
- Audio transcription and TTS
- Image analysis
- Conversation memory management

**Key Methods:**
- `ProcessMessageAsync()` - Main message processing pipeline
- `CanReplyAsync()` - Message filtering logic
- `AssignChatToAgentAsync()` - Human handoff functionality
- `GenerateResponseWithFunctionsAsync()` - AI response generation with function calling

### OpenAI Client (`Api/OpenAIClient.cs`)

OpenAI API integration:
- Chat completions with function calling
- Audio transcription (Whisper)
- Text-to-speech generation
- Image analysis (GPT-4V)
- Retry logic and error handling

**Key Methods:**
- `CreateChatCompletionAsync()` - Chat completion with tools
- `TranscribeAudioAsync()` - Audio to text conversion
- `GenerateSpeechAsync()` - Text to speech conversion
- `AnalyzeImageAsync()` - Image analysis

### Wassenger Client (`Api/WassengerClient.cs`)

WhatsApp API integration:
- Message sending (text, media, location, etc.)
- Device management and status checking
- Contact and chat operations
- Webhook registration
- Labels and metadata management

**Key Methods:**
- `SendMessageAsync()` - Send WhatsApp messages
- `LoadDeviceAsync()` - Device loading with caching
- `RegisterWebhookAsync()` - Webhook endpoint registration
- `DownloadMediaAsync()` - Media file downloads

### Configuration (`Config/BotConfig.cs`)

Centralized configuration management:
- Environment variable handling
- Default values and validation
- API configuration
- Bot behavior settings
- Feature toggles

### Memory Store (`Services/MemoryStore.cs`)

In-memory caching and state management:
- Conversation history storage
- Rate limiting counters
- Device and member caching
- Thread-safe operations

## Development Setup

### Prerequisites

1. **.NET 8 SDK**
   ```bash
   # macOS
   brew install --cask dotnet

   # Windows
   # Download from https://dotnet.microsoft.com/download/dotnet/8.0

   # Linux (Ubuntu/Debian)
   sudo apt-get install -y dotnet-sdk-8.0
   ```

2. **IDE/Editor** (optional but recommended)
   - Visual Studio 2022 (Windows/Mac)
   - Visual Studio Code with C# extension
   - JetBrains Rider

3. **Development Tools**
   - Ngrok (for local development)
   - Git
   - Postman or similar for API testing

### Initial Setup

1. **Clone and setup:**
   ```bash
   git clone <repository-url>
   cd whatsapp-chatgpt-bot-csharp
   ./setup.sh
   ```

2. **Configure environment:**
   ```bash
   cp .env.example .env
   # Edit .env with your API keys
   ```

3. **Test installation:**
   ```bash
   ./test.sh
   ```

## Project Structure

```
src/WhatsAppChatBot/
├── Program.cs                 # Application entry point
├── WhatsAppChatBot.csproj    # Project configuration
├── appsettings.json          # Application settings
├── appsettings.Development.json
│
├── Api/                      # External API clients
│   ├── OpenAIClient.cs       # OpenAI integration
│   └── WassengerClient.cs    # Wassenger integration
│
├── Bot/                      # Core bot logic
│   ├── ChatBot.cs           # Main bot implementation
│   └── FunctionHandler.cs   # Function calling logic
│
├── Config/                   # Configuration management
│   └── BotConfig.cs         # Centralized config
│
├── Controllers/              # HTTP controllers
│   └── WebhookController.cs # API endpoints
│
├── Models/                   # Data models
│   ├── OpenAIModels.cs      # OpenAI API models
│   ├── WassengerModels.cs   # Wassenger API models
│   └── WebhookModels.cs     # Webhook models
│
└── Services/                 # Business services
    ├── MemoryStore.cs       # Caching service
    └── NgrokTunnel.cs       # Development tunneling
```

## Configuration Management

### Environment Variables

The application uses environment variables for configuration:

```bash
# API Configuration
API_KEY=your_wassenger_api_key
OPENAI_API_KEY=your_openai_api_key
OPENAI_MODEL=gpt-4o

# Server Configuration
PORT=8080
WEBHOOK_URL=https://yourdomain.com/webhook
PRODUCTION=false
DEV=true

# Development
NGROK_TOKEN=your_ngrok_token
LOG_LEVEL=Information
```

### Configuration Classes

Configuration is managed through strongly-typed classes:

```csharp
// Main configuration class
public class BotConfig
{
    public ApiConfig Api { get; set; }
    public ServerConfig Server { get; set; }
    public FeaturesConfig Features { get; set; }
    public LimitsConfig Limits { get; set; }
    // ... more configs
}

// API configuration
public class ApiConfig
{
    public string ApiKey { get; set; }
    public string OpenAiKey { get; set; }
    public string OpenAiModel { get; set; }
}
```

### Dependency Injection

Services are registered in `Program.cs`:

```csharp
builder.Services.AddSingleton(botConfig);
builder.Services.AddSingleton<IMemoryStore, MemoryStore>();
builder.Services.AddSingleton<IChatBot, ChatBot>();
builder.Services.AddHttpClient<IOpenAIClient, OpenAIClient>();
```

## API Integration

### HTTP Client Configuration

HTTP clients are configured with retry policies:

```csharp
builder.Services.AddHttpClient<IOpenAIClient, OpenAIClient>()
    .AddPolicyHandler(GetRetryPolicy());

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
```

### OpenAI Integration

The OpenAI client supports:
- Chat completions with function calling
- Audio transcription (Whisper)
- Text-to-speech (TTS)
- Image analysis (GPT-4V)

```csharp
// Example: Chat completion with tools
var response = await _openAiClient.CreateChatCompletionAsync(
    messages,
    tools: functionTools);
```

### Wassenger Integration

The Wassenger client handles:
- Device management
- Message sending
- Webhook registration
- Media downloads

```csharp
// Example: Send a message
await _wassengerClient.SendMessageAsync(new SendMessageRequest
{
    Phone = data.FromNumber,
    Message = response,
    Device = device.Id
});
```

## Testing

### Automated Testing

Run the validation script:
```bash
./test.sh
```

This script validates:
- Project structure
- Dependencies
- Compilation
- Configuration

### Manual Testing

1. **API Endpoints:**
   ```bash
   # Health check
   curl http://localhost:8080/

   # Webhook test
   curl -X POST http://localhost:8080/webhook \
     -H "Content-Type: application/json" \
     -d '{"event":"message:in:new","data":{...}}'
   ```

2. **Message Flow:**
   - Send a WhatsApp message to your connected number
   - Check logs for processing steps
   - Verify response is received

### Unit Testing (Future Enhancement)

To add unit tests:

1. **Create test project:**
   ```bash
   dotnet new xunit -n WhatsAppChatBot.Tests
   dotnet add reference ../WhatsAppChatBot/WhatsAppChatBot.csproj
   ```

2. **Add test packages:**
   ```bash
   dotnet add package Moq
   dotnet add package Microsoft.AspNetCore.Mvc.Testing
   ```

3. **Example test structure:**
   ```csharp
   public class ChatBotTests
   {
       [Fact]
       public async Task ProcessMessage_ShouldReply_WhenValidMessage()
       {
           // Arrange
           var mockConfig = new Mock<BotConfig>();
           var chatBot = new ChatBot(mockConfig.Object, ...);

           // Act
           await chatBot.ProcessMessageAsync(testData, testDevice);

           // Assert
           // Verify expected behavior
       }
   }
   ```

## Deployment

### Local Development

```bash
# Run in development mode
cd src/WhatsAppChatBot
dotnet run

# Run with hot reload
dotnet watch run
```

### Docker Deployment

```bash
# Build image
docker build -t whatsapp-chatbot .

# Run container
docker run -d \
  --name whatsapp-chatbot \
  -p 8080:8080 \
  -e API_KEY=your_key \
  -e OPENAI_API_KEY=your_key \
  whatsapp-chatbot
```

### Cloud Deployment

#### Azure App Service

```bash
# Publish for deployment
dotnet publish -c Release -o ./publish

# Deploy to Azure
az webapp deployment source config-zip \
  --resource-group myResourceGroup \
  --name myapp \
  --src publish.zip
```

#### AWS Elastic Beanstalk

1. Publish application:
   ```bash
   dotnet publish -c Release
   ```

2. Create deployment package
3. Upload to Elastic Beanstalk

### Environment-Specific Configuration

#### Development
- Uses Ngrok for tunneling
- Detailed logging enabled
- Swagger UI available

#### Production
- Requires WEBHOOK_URL
- Minimal logging
- Health checks enabled

## Troubleshooting

### Common Issues

1. **Build Errors**
   ```bash
   # Clear and restore
   dotnet clean
   dotnet restore
   dotnet build
   ```

2. **Missing Dependencies**
   ```bash
   # Check project file
   cat WhatsAppChatBot.csproj

   # Restore specific package
   dotnet add package PackageName
   ```

3. **Configuration Issues**
   ```bash
   # Validate environment
   cat .env

   # Check configuration loading
   # Add logging in BotConfig.LoadFromEnvironment()
   ```

4. **API Connection Issues**
   ```bash
   # Test API connectivity
   curl -H "Authorization: Bearer $OPENAI_API_KEY" \
     https://api.openai.com/v1/models

   curl -H "Authorization: $API_KEY" \
     https://api.wassenger.com/v1/devices
   ```

### Debugging

1. **Enable detailed logging:**
   ```bash
   export LOG_LEVEL=Debug
   ```

2. **Use Visual Studio debugger:**
   - Set breakpoints in key methods
   - Inspect variable values
   - Step through execution

3. **Add custom logging:**
   ```csharp
   _logger.LogDebug("Processing message: {MessageId}", data.Id);
   ```

### Performance Monitoring

1. **Memory usage:**
   ```csharp
   // Monitor cache size
   var cacheSize = _memoryStore.GetAllData().Count;
   _logger.LogInformation("Cache size: {Size}", cacheSize);
   ```

2. **Response times:**
   ```csharp
   var stopwatch = Stopwatch.StartNew();
   await ProcessMessage(data);
   _logger.LogInformation("Processing took: {ElapsedMs}ms",
       stopwatch.ElapsedMilliseconds);
   ```

## Best Practices

### Code Organization
- Use dependency injection for all services
- Implement interfaces for testability
- Follow SOLID principles
- Use async/await for I/O operations

### Error Handling
- Use try-catch blocks appropriately
- Log errors with context
- Return meaningful error responses
- Implement circuit breaker pattern for external APIs

### Security
- Validate all inputs
- Use environment variables for secrets
- Implement rate limiting
- Secure webhook endpoints

### Performance
- Cache frequently accessed data
- Use connection pooling for HTTP clients
- Implement proper disposal patterns
- Monitor memory usage
