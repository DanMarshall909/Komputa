# API Integrations & Patterns

## OpenAI API Integration

### Current Implementation Analysis
```csharp
// Current basic implementation
public async Task<string> GetResponseAsync(string prompt)
{
    var request = new
    {
        model = "gpt-4",
        messages = new[] { new { role = "user", content = prompt } }
    };
    // ... rest of implementation
}
```

### Enhanced OpenAI Integration Patterns

#### 1. Conversation Context Management
```csharp
public class ConversationService
{
    private readonly List<ChatMessage> _conversationHistory = new();
    
    public async Task<string> GetResponseWithContextAsync(string userInput)
    {
        // Add user message to context
        _conversationHistory.Add(new ChatMessage("user", userInput));
        
        var request = new
        {
            model = _configuration["OpenAI:Model"] ?? "gpt-4",
            messages = _conversationHistory.ToArray(),
            max_tokens = 150,
            temperature = 0.7
        };
        
        // ... process response and add to history
        return response;
    }
}
```

#### 2. Error Handling & Resilience Patterns
```csharp
public async Task<string> GetResponseWithRetryAsync(string prompt, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "https://api.openai.com/v1/chat/completions", request);
                
            if (response.IsSuccessStatusCode)
            {
                return await ProcessSuccessfulResponse(response);
            }
            
            // Handle specific error codes
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = GetRetryAfterDelay(response);
                await Task.Delay(retryAfter);
                continue;
            }
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException("Invalid API key");
            }
            
        }
        catch (HttpRequestException ex) when (attempt < maxRetries)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
            await Task.Delay(delay);
        }
    }
    
    return "I'm sorry, I'm having trouble connecting right now. Please try again.";
}
```

#### 3. Cost Optimization Strategies
```csharp
public class CostOptimizedOpenAIService
{
    private readonly IMemoryCache _responseCache;
    private readonly TokenCountingService _tokenCounter;
    
    // Cache common responses
    public async Task<string> GetCachedResponseAsync(string prompt)
    {
        string cacheKey = GetCacheKey(prompt);
        
        if (_responseCache.TryGetValue(cacheKey, out string cachedResponse))
        {
            return cachedResponse;
        }
        
        // Estimate token usage before API call
        int estimatedTokens = _tokenCounter.EstimateTokens(prompt);
        if (estimatedTokens > _maxTokensPerRequest)
        {
            return await HandleLongPrompt(prompt);
        }
        
        var response = await GetResponseAsync(prompt);
        
        // Cache responses for common queries
        if (IsCommonQuery(prompt))
        {
            _responseCache.Set(cacheKey, response, TimeSpan.FromHours(1));
        }
        
        return response;
    }
}
```

### OpenAI Configuration Best Practices
```json
{
  "OpenAI": {
    "ApiKey": "", // Use environment variable: OPENAI_API_KEY
    "Model": "gpt-4",
    "MaxTokens": 150,
    "Temperature": 0.7,
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "RateLimitPerMinute": 60
  }
}
```

## Microsoft Cognitive Services Speech Integration

### Speech-to-Text Implementation Pattern
```csharp
public class SpeechRecognitionService
{
    private readonly SpeechConfig _speechConfig;
    private readonly AudioConfig _audioConfig;
    private SpeechRecognizer _recognizer;
    
    public async Task<string> RecognizeSpeechAsync()
    {
        try
        {
            var result = await _recognizer.RecognizeOnceAsync();
            
            switch (result.Reason)
            {
                case ResultReason.RecognizedSpeech:
                    return result.Text;
                    
                case ResultReason.NoMatch:
                    return "Speech could not be recognized.";
                    
                case ResultReason.Canceled:
                    var cancellation = CancellationDetails.FromResult(result);
                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        throw new InvalidOperationException(
                            $"Speech recognition error: {cancellation.ErrorDetails}");
                    }
                    return "Speech recognition was cancelled.";
                    
                default:
                    return "Unknown speech recognition result.";
            }
        }
        catch (Exception ex)
        {
            // Log error and provide fallback
            _logger.LogError(ex, "Speech recognition failed");
            return "Speech recognition error occurred.";
        }
    }
}
```

### Continuous Speech Recognition for Wake Word
```csharp
public class WakeWordDetectionService
{
    private SpeechRecognizer _continuousRecognizer;
    private readonly string _wakeWord = "hey komputa";
    
    public async Task StartContinuousRecognitionAsync()
    {
        _continuousRecognizer.Recognizing += OnRecognizing;
        _continuousRecognizer.Recognized += OnRecognized;
        _continuousRecognizer.Canceled += OnCanceled;
        
        await _continuousRecognizer.StartContinuousRecognitionAsync();
    }
    
    private void OnRecognized(object sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizedSpeech)
        {
            var recognizedText = e.Result.Text.ToLowerInvariant();
            
            if (recognizedText.Contains(_wakeWord))
            {
                OnWakeWordDetected?.Invoke(recognizedText);
                
                // Switch to command recognition mode
                _ = Task.Run(() => StartCommandRecognitionAsync());
            }
        }
    }
    
    public event Action<string> OnWakeWordDetected;
}
```

### Text-to-Speech Implementation Pattern
```csharp
public class TextToSpeechService
{
    private readonly SpeechSynthesizer _synthesizer;
    
    public async Task SpeakAsync(string text)
    {
        try
        {
            var result = await _synthesizer.SpeakTextAsync(text);
            
            switch (result.Reason)
            {
                case ResultReason.SynthesizingAudioCompleted:
                    // Speech synthesis succeeded
                    break;
                    
                case ResultReason.Canceled:
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    _logger.LogError($"Speech synthesis cancelled: {cancellation.ErrorDetails}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text-to-speech failed for text: {Text}", text);
            // Could implement fallback to system beep or text display
        }
    }
    
    public async Task SpeakWithVoiceAsync(string text, string voiceName)
    {
        var ssml = $@"
        <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
            <voice name='{voiceName}'>
                {text}
            </voice>
        </speak>";
        
        await _synthesizer.SpeakSsmlAsync(ssml);
    }
}
```

### Cognitive Services Configuration
```json
{
  "CognitiveServices": {
    "Speech": {
      "SubscriptionKey": "", // Use environment variable: COGNITIVE_SERVICES_KEY
      "Region": "eastus",
      "Language": "en-US",
      "Voice": "en-US-AriaNeural",
      "RecognitionLanguage": "en-US",
      "SpeechRecognitionLanguage": "en-US",
      "OutputFormat": "riff-24khz-16bit-mono-pcm"
    }
  }
}
```

## Integration Patterns & Best Practices

### Service Registration Pattern
```csharp
// Program.cs - Dependency Injection Setup
services.AddSingleton<IOpenAIService, OpenAIService>();
services.AddSingleton<ISpeechRecognitionService, SpeechRecognitionService>();
services.AddSingleton<ITextToSpeechService, TextToSpeechService>();
services.AddSingleton<IConversationService, ConversationService>();

// Configuration
services.Configure<OpenAIOptions>(config.GetSection("OpenAI"));
services.Configure<CognitiveServicesOptions>(config.GetSection("CognitiveServices"));
```

### Environment Variable Management
```csharp
public class SecureConfigurationService
{
    public static void LoadSecureConfiguration(IServiceCollection services)
    {
        // Load from environment variables for security
        var openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
            ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable not set");
            
        var cognitiveKey = Environment.GetEnvironmentVariable("COGNITIVE_SERVICES_KEY")
            ?? throw new InvalidOperationException("COGNITIVE_SERVICES_KEY environment variable not set");
            
        // Register with DI container
        services.AddSingleton(new OpenAICredentials(openAIKey));
        services.AddSingleton(new CognitiveServicesCredentials(cognitiveKey));
    }
}
```

### Logging Integration Pattern
```csharp
public class LoggingIntegratedService
{
    private readonly ILogger<LoggingIntegratedService> _logger;
    
    public async Task<string> ProcessRequestAsync(string input)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = Guid.NewGuid(),
            ["InputLength"] = input.Length,
            ["Timestamp"] = DateTime.UtcNow
        });
        
        _logger.LogInformation("Processing request with {InputLength} characters", input.Length);
        
        try
        {
            var startTime = DateTime.UtcNow;
            var result = await CallExternalAPIAsync(input);
            var duration = DateTime.UtcNow - startTime;
            
            _logger.LogInformation("Request processed successfully in {Duration}ms", 
                duration.TotalMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request processing failed");
            throw;
        }
    }
}
```

### Performance Monitoring
```csharp
public class PerformanceMetrics
{
    private readonly IMetricsCollector _metrics;
    
    public async Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await operation();
            _metrics.RecordSuccess(operationName, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _metrics.RecordFailure(operationName, stopwatch.ElapsedMilliseconds, ex.GetType().Name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }
}
```

## API Rate Limiting & Quotas

### OpenAI Rate Limits
- **GPT-4**: 10,000 tokens per minute (varies by subscription)
- **GPT-3.5-turbo**: 90,000 tokens per minute
- **Request Rate**: 3,500 requests per minute (typical)

### Cognitive Services Limits
- **Speech-to-Text**: 20 concurrent requests (Standard tier)
- **Text-to-Speech**: 200 requests per second
- **Custom Speech**: Varies by subscription

### Rate Limiting Implementation
```csharp
public class RateLimitingService
{
    private readonly SemaphoreSlim _rateLimiter;
    private readonly Queue<DateTime> _requestTimestamps = new();
    
    public async Task<bool> TryAcquireAsync(int maxRequestsPerMinute)
    {
        var now = DateTime.UtcNow;
        
        // Remove old timestamps
        while (_requestTimestamps.Count > 0 && 
               _requestTimestamps.Peek() < now.AddMinutes(-1))
        {
            _requestTimestamps.Dequeue();
        }
        
        if (_requestTimestamps.Count >= maxRequestsPerMinute)
        {
            return false; // Rate limit exceeded
        }
        
        _requestTimestamps.Enqueue(now);
        return true;
    }
}
