#!/bin/bash

# Quick verification script for project completeness
# This script checks if all required files exist without requiring .NET

echo "🔍 WhatsApp ChatGPT Bot C# - File Verification"
echo "=============================================="

# Define required files
required_files=(
    "README.md"
    "DEVELOPMENT.md"
    "VALIDATION_REPORT.md"
    ".env.example"
    ".gitignore"
    "Dockerfile"
    "setup.sh"
    "run.sh"
    "test.sh"
    "src/WhatsAppChatBot/WhatsAppChatBot.csproj"
    "src/WhatsAppChatBot/Program.cs"
    "src/WhatsAppChatBot/appsettings.json"
    "src/WhatsAppChatBot/appsettings.Development.json"
    "src/WhatsAppChatBot/Config/BotConfig.cs"
    "src/WhatsAppChatBot/Controllers/WebhookController.cs"
    "src/WhatsAppChatBot/Bot/ChatBot.cs"
    "src/WhatsAppChatBot/Bot/FunctionHandler.cs"
    "src/WhatsAppChatBot/Api/OpenAIClient.cs"
    "src/WhatsAppChatBot/Api/WassengerClient.cs"
    "src/WhatsAppChatBot/Services/MemoryStore.cs"
    "src/WhatsAppChatBot/Services/NgrokTunnel.cs"
    "src/WhatsAppChatBot/Models/WebhookModels.cs"
    "src/WhatsAppChatBot/Models/OpenAIModels.cs"
    "src/WhatsAppChatBot/Models/WassengerModels.cs"
)

# Check each file
missing_files=0
total_files=${#required_files[@]}

for file in "${required_files[@]}"; do
    if [ -f "$file" ]; then
        echo "✅ $file"
    else
        echo "❌ Missing: $file"
        ((missing_files++))
    fi
done

echo ""
echo "📊 Summary:"
echo "  Total files checked: $total_files"
echo "  Found: $((total_files - missing_files))"
echo "  Missing: $missing_files"

if [ $missing_files -eq 0 ]; then
    echo ""
    echo "🎉 All required files are present!"
    echo "✅ Project structure is complete and ready for testing"
    echo ""
    echo "Next steps:"
    echo "1. Install .NET 8 SDK: ./setup.sh"
    echo "2. Configure API keys: cp .env.example .env && nano .env"
    echo "3. Test the project: ./test.sh"
    echo "4. Run the bot: ./run.sh"
    exit 0
else
    echo ""
    echo "⚠️  Some files are missing. Please ensure all files are properly created."
    exit 1
fi
