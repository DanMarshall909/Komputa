using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Komputa.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Komputa.Services;

public class OpenAIProvider : ILanguageModelProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly bool _enableFunctionCalling;
    private readonly int _maxTokens;
    private readonly ILogger<OpenAIProvider> _logger;
    private readonly IWebSearchService _webSearchService;

    public string ProviderName => "OpenAI";

    public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

    public OpenAIProvider(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAIProvider> logger, IWebSearchService webSearchService)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
        _model = configuration["OpenAI:Model"] ?? "gpt-4o";
        _enableFunctionCalling = configuration.GetValue<bool>("OpenAI:EnableFunctionCalling", false);
        _maxTokens = configuration.GetValue<int>("OpenAI:MaxTokens", 1000);
        _logger = logger;
        _webSearchService = webSearchService;
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            _logger.LogInformation("OpenAI provider initialized with model {Model}, Function calling: {FunctionCalling}, Max tokens: {MaxTokens}", 
                _model, _enableFunctionCalling, _maxTokens);
        }
        else
        {
            _logger.LogWarning("OpenAI provider initialized but no API key found");
        }
    }

    public async Task<string> GetResponseAsync(string prompt, List<string>? context = null)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("OpenAI API call attempted but provider not available");
            return "OpenAI provider is not available - missing API key.";
        }

        _logger.LogInformation("Starting OpenAI API call with model {Model}", _model);
        _logger.LogDebug("User prompt: {Prompt}", prompt);

        var messages = new List<object> { new { role = "user", content = prompt } };
        
        if (context != null && context.Any())
        {
            _logger.LogInformation("Including {ContextCount} context items in request", context.Count);
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
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request);
            stopwatch.Stop();

            _logger.LogInformation("OpenAI API call completed in {ElapsedMs}ms with status {StatusCode}", 
                stopwatch.ElapsedMilliseconds, response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                return $"Error: {response.StatusCode} - {errorContent}";
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
            var responseContent = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response";
            
            _logger.LogInformation("OpenAI response received successfully, length: {ResponseLength} chars", responseContent.Length);
            _logger.LogDebug("AI response: {Response}", responseContent);
            
            return responseContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during OpenAI API call");
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
