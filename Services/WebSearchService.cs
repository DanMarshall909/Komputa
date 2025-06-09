using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.Net.Http.Json;

namespace Komputa.Services;

public interface IWebSearchService
{
    Task<string> SearchAsync(string query);
}

public class WebSearchService : IWebSearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebSearchService> _logger;

    public WebSearchService(HttpClient httpClient, ILogger<WebSearchService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> SearchAsync(string query)
    {
        _logger.LogInformation("Performing web search for query: {Query}", query);
        
        try
        {
            // Using DuckDuckGo Instant Answer API as a simple web search
            var url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&no_html=1&skip_disambig=1";
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(url);
            stopwatch.Stop();
            
            _logger.LogInformation("DuckDuckGo search completed in {ElapsedMs}ms with status {StatusCode}", 
                stopwatch.ElapsedMilliseconds, response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Search API returned error status: {StatusCode}", response.StatusCode);
                return $"Unable to search at this time. Please try again later.";
            }

            var result = await response.Content.ReadFromJsonAsync<DuckDuckGoResponse>();
            
            if (!string.IsNullOrEmpty(result?.AbstractText))
            {
                _logger.LogInformation("Found abstract result for query, length: {Length} chars", result.AbstractText.Length);
                return result.AbstractText;
            }
            
            if (result?.RelatedTopics?.Any() == true)
            {
                var topics = result.RelatedTopics.Take(3)
                    .Where(t => !string.IsNullOrEmpty(t.Text))
                    .Select(t => t.Text)
                    .ToList();
                
                if (topics.Any())
                {
                    _logger.LogInformation("Found {Count} related topics for query", topics.Count);
                    return $"Related information:\n{string.Join("\n", topics)}";
                }
            }
            
            _logger.LogInformation("No specific results found for query: {Query}", query);
            return $"I searched for '{query}' but couldn't find specific current information. You may want to check recent news sources directly.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during web search for query: {Query}", query);
            return "I'm unable to search the web right now. Please try again later.";
        }
    }
}

public class DuckDuckGoResponse
{
    [JsonPropertyName("AbstractText")] public string? AbstractText { get; set; }
    [JsonPropertyName("RelatedTopics")] public List<RelatedTopic>? RelatedTopics { get; set; }
}

public class RelatedTopic
{
    [JsonPropertyName("Text")] public string? Text { get; set; }
}
