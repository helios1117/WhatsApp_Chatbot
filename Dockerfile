# Use the official .NET 9.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official .NET 9.0 SDK as a build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/WhatsAppChatBot/WhatsAppChatBot.csproj", "WhatsAppChatBot/"]
RUN dotnet restore "WhatsAppChatBot/WhatsAppChatBot.csproj"
COPY src/ .
WORKDIR "/src/WhatsAppChatBot"
RUN dotnet build "WhatsAppChatBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WhatsAppChatBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV TEMP_PATH=/app/.tmp

ENTRYPOINT ["dotnet", "WhatsAppChatBot.dll"]
