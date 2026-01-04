using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhatsAppChatBot.Common;

namespace WhatsAppChatBot.Services;

public class MemoryStore : IUnifiedAction
{
    public Task<string> PerformAsync()
    {
        var dict = new Dictionary<string, string>
        {
            ["file"] = "src/WhatsAppChatBot/Services/MemoryStore.cs",
            ["class"] = "MemoryStore",
            ["result"] = "ok"
        };
        return Task.FromResult(JsonSerializer.Serialize(dict));
    }
}
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace WhatsAppChatBot.Services;

public interface IMemoryStore
{
    T? GetCache<T>(string key);
    void SetCache<T>(string key, T data, TimeSpan? ttl = null);
    void ClearCache(string key);
    void ClearAllCache();

    Dictionary<string, object> GetState(string chatId);
    void SetState(string chatId, Dictionary<string, object> state);
    void UpdateState(string chatId, Dictionary<string, object> data);
    void ClearState(string chatId);

    ChatStats GetStats(string chatId);
    int IncrementMessageCount(string chatId);
    bool HasChatMessagesQuota(string chatId, int maxMessages, int timeWindow);
    void ClearStats(string chatId);

    void ClearAll();
}

public class MemoryStore : IMemoryStore
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, Dictionary<string, object>> _state = new();
    private readonly ConcurrentDictionary<string, ChatStats> _stats = new();
    private readonly object _lock = new();

    public MemoryStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public T? GetCache<T>(string key)
    {
        return _cache.TryGetValue(key, out var value) && value is T result ? result : default;
    }

    public void SetCache<T>(string key, T data, TimeSpan? ttl = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (ttl.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = ttl;
        }
        else
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
        }

        _cache.Set(key, data, options);
    }

    public void ClearCache(string key)
    {
        _cache.Remove(key);
    }

    public void ClearAllCache()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Clear();
        }
    }

    public Dictionary<string, object> GetState(string chatId)
    {
        return _state.GetValueOrDefault(chatId, new Dictionary<string, object>());
    }

    public void SetState(string chatId, Dictionary<string, object> state)
    {
        _state[chatId] = new Dictionary<string, object>(state);
    }

    public void UpdateState(string chatId, Dictionary<string, object> data)
    {
        lock (_lock)
        {
            if (!_state.ContainsKey(chatId))
            {
                _state[chatId] = new Dictionary<string, object>();
            }

            foreach (var kvp in data)
            {
                _state[chatId][kvp.Key] = kvp.Value;
            }
        }
    }

    public void ClearState(string chatId)
    {
        _state.TryRemove(chatId, out _);
    }

    public ChatStats GetStats(string chatId)
    {
        return _stats.GetOrAdd(chatId, _ => new ChatStats
        {
            Messages = 0,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
    }

    public int IncrementMessageCount(string chatId)
    {
        lock (_lock)
        {
            var stats = GetStats(chatId);
            stats.Messages++;
            _stats[chatId] = stats;
            return stats.Messages;
        }
    }

    public bool HasChatMessagesQuota(string chatId, int maxMessages, int timeWindow)
    {
        lock (_lock)
        {
            var stats = GetStats(chatId);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Reset counter if time window has passed
            if (now - stats.Time > timeWindow)
            {
                _stats[chatId] = new ChatStats
                {
                    Messages = 0,
                    Time = now
                };
                return true;
            }

            return stats.Messages < maxMessages;
        }
    }

    public void ClearStats(string chatId)
    {
        _stats.TryRemove(chatId, out _);
    }

    public void ClearAll()
    {
        ClearAllCache();
        _state.Clear();
        _stats.Clear();
    }
}

public class ChatStats
{
    public int Messages { get; set; }
    public long Time { get; set; }
}

public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
}
