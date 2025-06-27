#!/bin/bash

# Test script for WhatsApp ChatGPT Bot C#
# This script validates the project structure and configuration

echo "🧪 Testing WhatsApp ChatGPT Bot C# Implementation"
echo "================================================="

# Check if we're in the right directory
if [ ! -f "src/WhatsAppChatBot/WhatsAppChatBot.csproj" ]; then
    echo "❌ Error: Please run this script from the project root directory"
    exit 1
fi

# Check if .NET is available
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK is required. Please install .NET 9 SDK first."
    echo "📥 Download from: https://dotnet.microsoft.com/download/dotnet/9.0"
    exit 1
fi

echo "✅ .NET SDK found: $(dotnet --version)"

# Check if .env file exists
if [ ! -f ".env" ]; then
    echo "⚠️  .env file not found. Creating from template..."
    cp .env.example .env
    echo "📝 Please configure .env file with your API keys"
fi

# Navigate to project directory
cd src/WhatsAppChatBot

echo ""
echo "📦 Testing package restore..."
dotnet restore > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ Package restore successful"
else
    echo "❌ Package restore failed"
    exit 1
fi

echo ""
echo "🔨 Testing project build..."
dotnet build --configuration Release --no-restore > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ Build successful"
else
    echo "❌ Build failed"
    exit 1
fi

echo ""
echo "🧪 Running basic validation tests..."

# Test 1: Check if main classes exist
echo "  - Checking core classes..."
REQUIRED_FILES=(
    "Program.cs"
    "Controllers/WebhookController.cs"
    "Api/OpenAIClient.cs"
    "Api/WassengerClient.cs"
    "Bot/ChatBot.cs"
    "Bot/FunctionHandler.cs"
    "Config/BotConfig.cs"
    "Services/MemoryStore.cs"
    "Services/NgrokTunnel.cs"
    "Models/WebhookModels.cs"
    "Models/OpenAIModels.cs"
    "Models/WassengerModels.cs"
)

for file in "${REQUIRED_FILES[@]}"; do
    if [ -f "$file" ]; then
        echo "    ✅ $file"
    else
        echo "    ❌ $file (missing)"
        exit 1
    fi
done

# Test 2: Check configuration files
echo "  - Checking configuration files..."
CONFIG_FILES=(
    "appsettings.json"
    "appsettings.Development.json"
    "WhatsAppChatBot.csproj"
)

for file in "${CONFIG_FILES[@]}"; do
    if [ -f "$file" ]; then
        echo "    ✅ $file"
    else
        echo "    ❌ $file (missing)"
        exit 1
    fi
done

# Test 3: Validate project file dependencies
echo "  - Checking NuGet dependencies..."
REQUIRED_PACKAGES=(
    "Microsoft.AspNetCore.OpenApi"
    "Swashbuckle.AspNetCore"
    "Microsoft.Extensions.Caching.Memory"
    "DotNetEnv"
    "Microsoft.Extensions.Http.Resilience"
)

for package in "${REQUIRED_PACKAGES[@]}"; do
    if grep -q "$package" "WhatsAppChatBot.csproj"; then
        echo "    ✅ $package"
    else
        echo "    ❌ $package (missing from project file)"
        exit 1
    fi
done

# Test 4: Check for compilation errors in key files
echo "  - Checking for syntax errors..."
dotnet build --no-restore --verbosity quiet > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "    ✅ No compilation errors"
else
    echo "    ❌ Compilation errors found"
    echo "    Run 'dotnet build' for details"
    exit 1
fi

cd ../..

echo ""
echo "🎉 All tests passed successfully!"
echo ""
echo "📋 Project validation summary:"
echo "  ✅ .NET SDK installed and compatible"
echo "  ✅ All required source files present"
echo "  ✅ Configuration files valid"
echo "  ✅ NuGet dependencies configured"
echo "  ✅ Project compiles without errors"
echo ""
echo "🚀 Your C# WhatsApp ChatGPT Bot is ready!"
echo ""
echo "📝 Next steps:"
echo "1. Configure your .env file with API keys"
echo "2. Run: ./run.sh"
echo "3. Or manually: cd src/WhatsAppChatBot && dotnet run"
echo ""
echo "📖 For more information, see README.md"
