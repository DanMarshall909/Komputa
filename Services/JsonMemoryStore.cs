using System.Text.Json;
using Komputa.Interfaces;
using Komputa.Models;

namespace Komputa.Services;

public class JsonMemoryStore : IMemoryStore
{
    private readonly string _filePath;
    private readonly List<MemoryItem> _memories;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonMemoryStore(string filePath = "memories.json")
    {
        _filePath = filePath;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        _memories = LoadMemories();
    }

    public async Task AddMemoryAsync(MemoryItem item)
    {
        _memories.Add(item);
        await SaveAsync();
    }

    public Task<IEnumerable<MemoryItem>> GetRecentMemoriesAsync(int count)
    {
        var recent = _memories
            .OrderByDescending(m => m.Timestamp)
            .Take(count);
        
        return Task.FromResult(recent);
    }

    public Task<IEnumerable<MemoryItem>> SearchAsync(string query, int limit)
    {
        var searchResults = _memories
            .Where(m => m.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       m.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.Timestamp)
            .Take(limit);
        
        return Task.FromResult(searchResults);
    }

    public Task<IEnumerable<MemoryItem>> GetTopMemoriesAsync(int count)
    {
        var topMemories = _memories
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.Timestamp)
            .Take(count);
        
        return Task.FromResult(topMemories);
    }

    public async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_memories, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    private List<MemoryItem> LoadMemories()
    {
        if (!File.Exists(_filePath))
        {
            return new List<MemoryItem>();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<MemoryItem>>(json, _jsonOptions) ?? new List<MemoryItem>();
        }
        catch
        {
            // If file is corrupted, start fresh
            return new List<MemoryItem>();
        }
    }
}
