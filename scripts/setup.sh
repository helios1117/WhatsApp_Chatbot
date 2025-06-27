#!/bin/bash

# Development Setup Script for WhatsApp ChatGPT Bot C#

echo "🔧 Setting up WhatsApp ChatGPT Bot C# Development Environment"
echo "==========================================================="

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check and install .NET 9 SDK
echo "1. Checking .NET 9 SDK..."
if command_exists dotnet; then
    DOTNET_VERSION=$(dotnet --version)
    echo "✅ .NET SDK found: $DOTNET_VERSION"

    # Check if it's .NET 9.x
    if [[ $DOTNET_VERSION == 9.* ]]; then
        echo "✅ .NET 9 SDK is installed"
    else
        echo "⚠️  .NET 9 SDK is recommended (current: $DOTNET_VERSION)"
    fi
else
    echo "❌ .NET SDK not found"
    echo "📥 Installing .NET 9 SDK..."

    # Detect OS and install accordingly
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        if command_exists brew; then
            brew install --cask dotnet
        else
            echo "Please install Homebrew first or download .NET manually from:"
            echo "https://dotnet.microsoft.com/download/dotnet/9.0"
            exit 1
        fi
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        # Linux
        echo "Please install .NET 9 SDK using your package manager:"
        echo "Ubuntu/Debian: sudo apt-get install -y dotnet-sdk-9.0"
        echo "Or download from: https://dotnet.microsoft.com/download/dotnet/9.0"
        exit 1
    else
        echo "Please download and install .NET 9 SDK from:"
        echo "https://dotnet.microsoft.com/download/dotnet/9.0"
        exit 1
    fi
fi

# Check for ngrok (optional but recommended for development)
echo ""
echo "2. Checking Ngrok..."
if command_exists ngrok; then
    echo "✅ Ngrok found: $(ngrok version)"
else
    echo "⚠️  Ngrok not found (optional for development)"
    echo "📥 To install Ngrok:"

    if [[ "$OSTYPE" == "darwin"* ]]; then
        echo "  macOS: brew install ngrok/ngrok/ngrok"
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        echo "  Linux: Download from https://ngrok.com/download"
    fi

    echo "  Or download from: https://ngrok.com/download"
fi

# Create .env file if it doesn't exist
echo ""
echo "3. Setting up environment configuration..."
if [ ! -f ".env" ]; then
    cp .env.example .env
    echo "✅ Created .env file from template"
    echo "📝 Please edit .env file with your API keys"
else
    echo "✅ .env file already exists"
fi

# Restore NuGet packages
echo ""
echo "4. Restoring NuGet packages..."
cd src/WhatsAppChatBot
dotnet restore

if [ $? -eq 0 ]; then
    echo "✅ NuGet packages restored successfully"
else
    echo "❌ Failed to restore NuGet packages"
    exit 1
fi

# Build the project
echo ""
echo "5. Building the project..."
dotnet build

if [ $? -eq 0 ]; then
    echo "✅ Project built successfully"
else
    echo "❌ Build failed"
    exit 1
fi

# Create temporary directory
echo ""
echo "6. Setting up temporary directory..."
mkdir -p .tmp
echo "✅ Temporary directory created"

cd ../..

echo ""
echo "🎉 Setup completed successfully!"
echo ""
echo "📋 Next steps:"
echo "1. Edit .env file with your API keys:"
echo "   - API_KEY: Get from https://app.wassenger.com/apikeys"
echo "   - OPENAI_API_KEY: Get from https://platform.openai.com/account/api-keys"
echo "   - NGROK_TOKEN: Get from https://dashboard.ngrok.com/get-started/your-authtoken"
echo ""
echo "2. Run the bot:"
echo "   ./run.sh"
echo ""
echo "3. Or run manually:"
echo "   cd src/WhatsAppChatBot && dotnet run"
echo ""
echo "📖 For more information, see README.md"
