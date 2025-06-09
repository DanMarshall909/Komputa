using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Komputa.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Komputa.Services;

public class OpenAIProvider : ILanguageModelProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public string ProviderName => "OpenAI";

    public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

    public OpenAIProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
        _model = configuration["OpenAI:Model"] ?? "gpt-4";
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    public async Task<string> GetResponseAsync(string prompt, List<string>? context = null)
    {
        if (!IsAvailable)
        {
            return "OpenAI provider is not available - missing API key.";
        }

        var messages = new List<object> { new { role = "user", content = prompt } };
        
        if (context != null && context.Any())
        {
            var contextPrompt = "Here's some relevant context from our previous conversations:\n" + 
                               string.Join("\n", context) + "\n\nUser: " + prompt;
            messages = new List<object> { new { role = "user", content = contextPrompt } };
        }

        var request = new
        {
            model = _model,
            messages = messages
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return $"Error: {response.StatusCode} - {errorContent}";
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response";
        }
        catch (Exception ex)
        {
            return $"Error calling OpenAI: {ex.Message}";
        }
    }

    public async Task<string> GetResponseWithContextAsync(string prompt, string conversationContext)
    {
        if (!IsAvailable)
        {
            return "OpenAI provider is not available - missing API key.";
        }

        var contextualPrompt = !string.IsNullOrEmpty(conversationContext) 
            ? $"Context from previous conversations:\n{conversationContext}\n\nUser: {prompt}"
            : prompt;

        return await GetResponseAsync(contextualPrompt);
    }
}

public class OpenAiResponse
{
    [JsonPropertyName("choices")] public List<Choice>? Choices { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")] public Message? Message { get; set; }
}

public class Message
{
    [JsonPropertyName("content")] public string? Content { get; set; }
}
