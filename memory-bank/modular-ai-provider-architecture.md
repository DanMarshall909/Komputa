# Modular AI Provider Architecture


## Overview: Provider-Agnostic Intelligence

Komputa will implement a modular AI provider system that supports multiple language model providers including OpenAI, Ollama (local), Azure OpenAI, Anthropic Claude, Google Gemini, and others. This architecture ensures flexibility, cost optimization, and offline capability.

## Core Architecture Principles

### Provider Abstraction Layer
```csharp
public interface ILanguageModelProvider
{
    string ProviderName { get; }
    bool IsAvailable { get; }
    bool SupportsStreaming { get; }
    bool RequiresInternet { get; }
    
    Task<string> GenerateResponseAsync(string prompt, LLMOptions options = null);
    Task<string> GenerateResponseAsync(IEnumerable<ChatMessage> conversation, LLMOptions options = null);
    IAsyncEnumerable<string> GenerateStreamingResponseAsync(string prompt, LLMOptions options = null);
    Task<bool> ValidateConnectionAsync();
}

public class LLMOptions
{
    public string Model { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 150;
    public int TimeoutSeconds { get; set; } = 30;
    public Dictionary<string, object> ProviderSpecificOptions { get; set; } = new();
}
```

### Provider Factory Pattern
```csharp
public interface ILanguageModelProviderFactory
{
    ILanguageModelProvider CreateProvider(string providerName);
    IEnumerable<string> GetAvailableProviders();
    ILanguageModelProvider GetBestAvailableProvider();
    ILanguageModelProvider GetPreferredProvider(string[] preferences);
}

public class LanguageModelProviderFactory : ILanguageModelProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderConfiguration _config;
    
    public ILanguageModelProvider GetBestAvailableProvider()
    {
        // Priority: Local providers first (faster, private), then cloud providers
        var providers = new[]
        {
            "Ollama",
            "LMStudio", 
            "OpenAI",
            "AzureOpenAI",
            "Anthropic",
            "GoogleGemini"
        };
        
        foreach (var providerName in providers)
        {
            var provider = CreateProvider(providerName);
            if (provider?.IsAvailable == true)
            {
                return provider;
            }
        }
        
        throw new InvalidOperationException("No language model providers available");
    }
}
```

## Supported AI Providers

### 1. Ollama (Local AI) - Primary Recommendation
```csharp
public class OllamaProvider : ILanguageModelProvider
{
    public string ProviderName => "Ollama";
    public bool RequiresInternet => false;
    public bool SupportsStreaming => true;
    
    private readonly HttpClient _httpClient;
    private readonly OllamaConfiguration _config;
    
    public async Task<string> GenerateResponseAsync(string prompt, LLMOptions options = null)
    {
        var request = new
        {
            model = options?.Model ?? _config.DefaultModel ?? "llama3:latest",
            prompt = prompt,
            stream = false,
            options = new
            {
                temperature = options?.Temperature ?? 0.7,
                num_predict = options?.MaxTokens ?? 150
            }
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            $"{_config.BaseUrl}/api/generate", request);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Ollama request failed: {response.StatusCode}");
        }
        
        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
        return result.Response;
    }
    
    public async Task<bool> ValidateConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_config.BaseUrl}/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

public class OllamaConfiguration
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string DefaultModel { get; set; } = "llama3:latest";
    public int TimeoutSeconds { get; set; } = 30;
    public bool AutoPullModels { get; set; } = true;
    public string[] PreferredModels { get; set; } = { "llama3:latest", "mistral:latest", "codellama:latest" };
}
```

### 2. OpenAI Provider (Enhanced)
```csharp
public class OpenAIProvider : ILanguageModelProvider
{
    public string ProviderName => "OpenAI";
    public bool RequiresInternet => true;
    public bool SupportsStreaming => true;
    
    public async Task<string> GenerateResponseAsync(IEnumerable<ChatMessage> conversation, LLMOptions options = null)
    {
        var request = new
        {
            model = options?.Model ?? _config.DefaultModel ?? "gpt-4",
            messages = conversation.Select(m => new { role = m.Role, content = m.Content }),
            max_tokens = options?.MaxTokens ?? 150,
            temperature = options?.Temperature ?? 0.7
        };
        
        // Implementation with retry logic, rate limiting, etc.
        return await SendRequestWithRetry(request);
    }
}
```

### 3. LM Studio Provider (Local)
```csharp
public class LMStudioProvider : ILanguageModelProvider
{
    public string ProviderName => "LMStudio";
    public bool RequiresInternet => false;
    public bool SupportsStreaming => true;
    
    // Similar to OpenAI API but for local LM Studio server
    private readonly string _baseUrl = "http://localhost:1234";
    
    public async Task<string> GenerateResponseAsync(string prompt, LLMOptions options = null)
    {
        // LM Studio uses OpenAI-compatible API
        var request = new
        {
            model = options?.Model ?? "local-model",
            messages = new[] { new { role = "user", content = prompt } },
            temperature = options?.Temperature ?? 0.7,
            max_tokens = options?.MaxTokens ?? 150
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/v1/chat/completions", request);
            
        // Process response similar to OpenAI
        return await ProcessOpenAICompatibleResponse(response);
    }
}
```

### 4. Azure OpenAI Provider
```csharp
public class AzureOpenAIProvider : ILanguageModelProvider
{
    public string ProviderName => "AzureOpenAI";
    public bool RequiresInternet => true;
    
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deploymentName;
    
    // Azure-specific implementation with deployment names and regional endpoints
}
```

### 5. Anthropic Claude Provider
```csharp
public class AnthropicProvider : ILanguageModelProvider
{
    public string ProviderName => "Anthropic";
    public bool RequiresInternet => true;
    
    // Anthropic-specific API implementation
}
```

## Configuration System

### Provider Configuration
```json
{
  "AIProviders": {
    "DefaultProvider": "Ollama",
    "FallbackOrder": ["Ollama", "LMStudio", "OpenAI", "AzureOpenAI"],
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "DefaultModel": "llama3:latest",
      "PreferredModels": ["llama3:latest", "mistral:latest", "codellama:latest"],
      "TimeoutSeconds": 30,
      "AutoPullModels": true
    },
    "LMStudio": {
      "BaseUrl": "http://localhost:1234",
      "DefaultModel": "local-model",
      "TimeoutSeconds": 30
    },
    "OpenAI": {
      "ApiKey": "", // Use environment variable
      "Model": "gpt-4",
      "MaxTokens": 150,
      "Temperature": 0.7,
      "TimeoutSeconds": 30
    },
    "AzureOpenAI": {
      "Endpoint": "",
      "ApiKey": "",
      "DeploymentName": "",
      "ApiVersion": "2023-12-01-preview"
    },
    "Anthropic": {
      "ApiKey": "",
      "Model": "claude-3-sonnet-20240229",
      "MaxTokens": 150
    }
  }
}
```

### Provider Selection Strategy
```csharp
public class ProviderSelectionStrategy
{
    public async Task<ILanguageModelProvider> SelectProviderAsync(
        ConversationContext context, 
        UserPreferences preferences)
    {
        // Selection criteria:
        // 1. User preference (local vs cloud)
        // 2. Privacy requirements (sensitive topics -> local only)
        // 3. Performance requirements (speed vs quality)
        // 4. Cost considerations (local free, cloud paid)
        // 5. Availability (offline scenarios)
        
        if (preferences.PreferLocalAI || context.ContainsSensitiveInfo)
        {
            return await GetBestLocalProvider();
        }
        
        if (preferences.PreferHighQuality && NetworkAvailable())
        {
            return await GetBestCloudProvider();
        }
        
        return await _factory.GetBestAvailableProvider();
    }
    
    private async Task<ILanguageModelProvider> GetBestLocalProvider()
    {
        var localProviders = new[] { "Ollama", "LMStudio" };
        
        foreach (var providerName in localProviders)
        {
            var provider = _factory.CreateProvider(providerName);
            if (await provider.ValidateConnectionAsync())
            {
                return provider;
            }
        }
        
        return null; // No local providers available
    }
}
```

## Voice Assistant Integration

### Memory-Aware Provider Selection
```csharp
public class VoiceAssistantService
{
    private readonly ILanguageModelProviderFactory _providerFactory;
    private readonly IMemoryStore _memoryStore;
    private readonly ProviderSelectionStrategy _selectionStrategy;
    
    public async Task<string> ProcessVoiceCommandAsync(string userInput)
    {
        // 1. Analyze conversation context from memory
        var context = await _memoryStore.BuildContextAsync(userInput);
        
        // 2. Select appropriate AI provider based on context
        var provider = await _selectionStrategy.SelectProviderAsync(context, GetUserPreferences());
        
        // 3. Build contextual prompt with memory
        var contextualPrompt = BuildPromptWithMemory(userInput, context);
        
        // 4. Generate response using selected provider
        var response = await provider.GenerateResponseAsync(contextualPrompt);
        
        // 5. Store interaction in memory
        await StoreInteractionInMemory(userInput, response, provider.ProviderName);
        
        return response;
    }
}
```

## Benefits of Modular Architecture

### Cost Optimization
- **Local AI**: Free unlimited usage with Ollama/LM Studio
- **Hybrid Approach**: Local for simple queries, cloud for complex tasks
- **Provider Competition**: Switch between providers based on pricing

### Privacy & Security
- **Local Processing**: Sensitive conversations stay on device
- **No Data Transmission**: Complete offline capability with local models
- **User Control**: Choose privacy level per conversation

### Performance & Reliability
- **Automatic Fallback**: If primary provider fails, automatically try alternatives
- **Offline Operation**: Continue working without internet connection
- **Response Time Optimization**: Local providers for faster responses

### Flexibility & Future-Proofing
- **Easy Provider Addition**: Add new providers without core changes
- **Model Variety**: Access to different models for different tasks
- **Vendor Independence**: Not locked into single provider ecosystem

## Implementation Priority

### Phase 1: Core Architecture (Week 1-2)
- Implement ILanguageModelProvider interface
- Create provider factory and selection strategy
- Basic Ollama and OpenAI implementations

### Phase 2: Enhanced Providers (Week 2-3)
- Add LM Studio support
- Implement Azure OpenAI provider
- Add Anthropic Claude provider

### Phase 3: Intelligent Selection (Week 3-4)
- Memory-aware provider selection
- Context-based privacy routing
- Performance optimization

### Phase 4: Advanced Features (Week 4-5)
- Streaming responses for all providers
- Provider health monitoring
- Automatic model management for Ollama

This modular architecture ensures Komputa can leverage the best AI technology available while maintaining user privacy, minimizing costs, and providing reliable offline operation.
