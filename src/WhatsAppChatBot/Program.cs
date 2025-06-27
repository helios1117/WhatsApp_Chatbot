using WhatsAppChatBot.Api;
using WhatsAppChatBot.Bot;
using WhatsAppChatBot.Config;
using WhatsAppChatBot.Models;
using WhatsAppChatBot.Services;
using DotNetEnv;

namespace WhatsAppChatBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Load environment variables
        if (File.Exists(".env"))
        {
            Env.Load();
        }

        var builder = WebApplication.CreateBuilder(args);

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(GetLogLevel());

        // Load and validate configuration
        var botConfig = BotConfigExtensions.LoadFromEnvironment();
        botConfig.ValidateConfig();
        builder.Services.AddSingleton(botConfig);

        // Configure services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddMemoryCache();

        // Register HTTP clients with retry policies
        builder.Services.AddHttpClient<IOpenAIClient, OpenAIClient>()
            .AddStandardResilienceHandler();

        builder.Services.AddHttpClient<IWassengerClient, WassengerClient>()
            .AddStandardResilienceHandler();

        // Register services
        builder.Services.AddSingleton<IMemoryStore, MemoryStore>();
        builder.Services.AddSingleton<IFunctionHandler, FunctionHandler>();
        builder.Services.AddSingleton<IChatBot, ChatBot>();
        builder.Services.AddSingleton<INgrokTunnel>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<NgrokTunnel>>();
            var token = botConfig.Server.NgrokToken ?? string.Empty;
            return new NgrokTunnel(token, logger);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.MapControllers();

        // Initialize bot services (required for both development and production)
        Console.WriteLine("📋 Initializing bot services...");
        await InitializeBotServicesAsync(app.Services, botConfig);
        Console.WriteLine("🎯 Bot services initialized successfully!");

        // Check if running in development mode
        var isDevelopment = Environment.GetEnvironmentVariable("DEV")?.ToLower() == "true";

        if (isDevelopment)
        {
            Console.WriteLine("🚀 Starting ChatGPT WhatsApp Bot in development mode...");

            var ngrokTunnel = app.Services.GetRequiredService<INgrokTunnel>();
            ngrokTunnel.RegisterShutdownHandler();

            Console.WriteLine($"🚀 Starting development server on http://localhost:{botConfig.Server.Port}");
            Console.WriteLine("📋 Server logs will appear below. Press Ctrl+C to stop.\n");
        }
        else
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("🚀 ChatGPT WhatsApp Bot started in production mode");
            logger.LogInformation("Make sure the web server can handle POST requests to /webhook on port {Port}", botConfig.Server.Port);
        }

        app.Run($"http://0.0.0.0:{botConfig.Server.Port}");
    }

    private static async Task InitializeBotServicesAsync(IServiceProvider services, BotConfig config)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var chatBot = scope.ServiceProvider.GetRequiredService<IChatBot>();
        var ngrokTunnel = scope.ServiceProvider.GetRequiredService<INgrokTunnel>();

        try
        {
            Console.WriteLine("🔧 Loading configuration...");
            // Configuration already loaded and validated

            Console.WriteLine("📁 Creating temporary directory...");
            CreateTempDirectory(config.Server.TempPath);

            Console.WriteLine("🤖 Initializing ChatBot...");
            // ChatBot already initialized through DI

            Console.WriteLine("📱 Loading WhatsApp device...");
            var device = await InitializeBotAsync(chatBot, config);

            Console.WriteLine("🏷️ Setting up labels and members...");
            await SetupLabelsAndMembersAsync(chatBot, device, config);

            Console.WriteLine("🔗 Setting up webhook...");
            await SetupWebhookAsync(chatBot, device, config, ngrokTunnel);

            logger.LogInformation("Bot services initialized successfully");
            Console.WriteLine("✅ Bot services initialization completed!");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to initialize bot services");
            Environment.Exit(1);
        }
    }

    private static async Task<WassengerDevice> InitializeBotAsync(IChatBot bot, BotConfig config)
    {
        var wassengerClient = bot.GetWassengerClient();
        var device = await wassengerClient.LoadDeviceAsync(config.Server.Device);

        if (device == null || device.Status != "operative")
        {
            throw new InvalidOperationException("No active WhatsApp numbers in your account. Please connect a WhatsApp number in your Wassenger account: https://app.wassenger.com/create");
        }

        if (device.Session?.Status != "online")
        {
            throw new InvalidOperationException($"WhatsApp number ({device.Alias}) is not online. Please make sure the WhatsApp number in your Wassenger account is properly connected: https://app.wassenger.com/{device.Id}/scan");
        }

        var billingProduct = device.Billing?.Subscription?.Product;
        if (billingProduct != "io")
        {
            throw new InvalidOperationException($"WhatsApp number plan ({device.Alias}) does not support inbound messages. Please upgrade the plan here: https://app.wassenger.com/{device.Id}/plan?product=io");
        }

        Console.WriteLine($"Using WhatsApp connected number: phone={device.Phone}, alias={device.Alias}, id={device.Id}");
        return device;
    }

    private static async Task SetupLabelsAndMembersAsync(IChatBot bot, WassengerDevice device, BotConfig config)
    {
        try
        {
            var wassengerClient = bot.GetWassengerClient();

            // Pre-load device labels and team members
            await wassengerClient.PullMembersAsync(device);
            await wassengerClient.PullLabelsAsync(device);

            // Create labels if they don't exist
            var requiredLabels = new List<string>();
            requiredLabels.AddRange(config.SetLabelsOnBotChats);
            requiredLabels.AddRange(config.SetLabelsOnUserAssignment);

            if (requiredLabels.Any())
            {
                await wassengerClient.CreateLabelsAsync(device, requiredLabels);
            }

            Console.WriteLine("Labels and members setup completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to setup labels and members: {ex.Message}");
        }
    }

    private static async Task SetupWebhookAsync(IChatBot bot, WassengerDevice device, BotConfig config, INgrokTunnel ngrokTunnel)
    {
        var wassengerClient = bot.GetWassengerClient();

        if (config.Server.Production)
        {
            Console.WriteLine("Validating webhook endpoint...");

            var webhookUrl = config.Server.WebhookUrl;
            if (string.IsNullOrEmpty(webhookUrl))
            {
                throw new InvalidOperationException("Webhook URL is required for production mode. Please set WEBHOOK_URL environment variable");
            }

            var webhook = await wassengerClient.RegisterWebhookAsync(webhookUrl, device);
            if (webhook == null)
            {
                throw new InvalidOperationException("Failed to register webhook in production mode");
            }

            Console.WriteLine($"Using webhook endpoint in production mode: {webhook.Url}");
        }
        else
        {
            Console.WriteLine("Registering webhook tunnel...");

            var tunnelUrl = config.Server.WebhookUrl;
            if (string.IsNullOrEmpty(tunnelUrl))
            {
                if (string.IsNullOrEmpty(config.Server.NgrokToken))
                {
                    throw new InvalidOperationException("Ngrok token is required for development mode. Get one from: https://ngrok.com/signup");
                }

                tunnelUrl = await ngrokTunnel.CreateAsync(config.Server.Port);
            }

            var webhookUrl = $"{tunnelUrl}/webhook";
            var webhook = await wassengerClient.RegisterWebhookAsync(webhookUrl, device);
            if (webhook == null)
            {
                throw new InvalidOperationException("Failed to register webhook tunnel");
            }

            Console.WriteLine($"Webhook tunnel registered: {webhookUrl}");
        }
    }

    private static void CreateTempDirectory(string tempPath)
    {
        if (!Directory.Exists(tempPath))
        {
            try
            {
                Directory.CreateDirectory(tempPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create temporary directory: {tempPath} - {ex.Message}");
            }
        }
    }

    private static LogLevel GetLogLevel()
    {
        var logLevelStr = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";
        return Enum.TryParse<LogLevel>(logLevelStr, true, out var logLevel) ? logLevel : LogLevel.Information;
    }
}
