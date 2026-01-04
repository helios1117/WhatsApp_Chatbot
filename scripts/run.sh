#!/bin/bash

# WhatsApp ChatGPT Bot - C# Launch Script

echo "WhatsApp ChatGPT Bot - C# Edition"
echo "======================================"

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET 9 SDK is required but not installed."
    echo "📥 Please install .NET 9 SDK from: https://dotnet.microsoft.com/download/dotnet/9.0"
    echo ""
    echo "Installation commands:"
    echo "  macOS (Homebrew): brew install --cask dotnet"
    echo "  Ubuntu/Debian: sudo apt-get install -y dotnet-sdk-9.0"
    echo "  Windows: Download from Microsoft website"
    exit 1
fi

# Check .NET version
echo "✅ .NET version: $(dotnet --version)"

# Check if .env file exists
if [ ! -f ".env" ]; then
    echo "⚠️  .env file not found. Copying from .env.example..."
    cp .env.example .env
    echo "📝 Please edit .env file with your API keys before running the bot."
    echo ""
    echo "Required configuration:"
    echo "  - API_KEY: Your Wassenger API key"
    echo "  - OPENAI_API_KEY: Your OpenAI API key"
    echo "  - NGROK_TOKEN: Your Ngrok token (for development)"
    echo ""
    echo "Get your API keys from:"
    echo "  - Wassenger: https://app.wassenger.com/apikeys"
    echo "  - OpenAI: https://platform.openai.com/account/api-keys"
    echo "  - Ngrok: https://dashboard.ngrok.com/get-started/your-authtoken"
    exit 1
fi

# Restore packages
echo "📦 Restoring NuGet packages..."
cd src/WhatsAppChatBot
dotnet restore

if [ $? -ne 0 ]; then
    echo "❌ Failed to restore packages"
    exit 1
fi

# Build project
echo "🔨 Building project..."
dotnet build

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo "✅ Build successful!"
echo ""
echo "🚀 Starting WhatsApp ChatGPT Bot..."
echo "📋 Make sure your .env file is configured with valid API keys"
echo "🌐 Bot will be available at: http://localhost:8080"
echo "📋 Press Ctrl+C to stop the bot"
echo ""

# Run the application
dotnet run
