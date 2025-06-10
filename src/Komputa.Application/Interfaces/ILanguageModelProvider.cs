using Komputa.Domain.Models;

namespace Komputa.Application.Interfaces;

public interface ILanguageModelProvider
{
    string ProviderName { get; }
    Task<string> GetResponseAsync(string prompt, List<string>? context = null);
    Task<string> GetResponseWithContextAsync(string prompt, string conversationContext);
    bool IsAvailable { get; }
}

public interface IMemoryStore
{
    Task AddMemoryAsync(MemoryItem item);
    Task<IEnumerable<MemoryItem>> GetRecentMemoriesAsync(int count);
    Task<IEnumerable<MemoryItem>> SearchAsync(string query, int limit);
    Task<IEnumerable<MemoryItem>> GetTopMemoriesAsync(int count);
    Task SaveAsync();
}

public interface IContentScorer
{
    double ScoreContent(string content, string contentType);
}
