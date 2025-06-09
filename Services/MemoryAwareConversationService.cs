using Komputa.Interfaces;
using Komputa.Models;
using Microsoft.Extensions.Logging;

namespace Komputa.Services;

public class MemoryAwareConversationService
{
    private readonly IMemoryStore _memoryStore;
    private readonly ILanguageModelProvider _aiProvider;
    private readonly IContentScorer _contentScorer;
    private readonly IWebSearchService _webSearchService;
    private readonly ILogger<MemoryAwareConversationService> _logger;
    private readonly string _conversationId;

    public MemoryAwareConversationService(
        IMemoryStore memoryStore,
        ILanguageModelProvider aiProvider,
        IContentScorer contentScorer,
        IWebSearchService webSearchService,
        ILogger<MemoryAwareConversationService> logger)
    {
        _memoryStore = memoryStore;
        _aiProvider = aiProvider;
        _contentScorer = contentScorer;
        _webSearchService = webSearchService;
        _logger = logger;
        _conversationId = Guid.NewGuid().ToString();
    }

    public async Task<string> GetResponseWithMemoryAsync(string userInput)
    {
        _logger.LogInformation("Processing user input: {Input}", userInput);

        // 1. Store user input as memory item
        await StoreUserInputAsync(userInput);

        // 2. Check if web search is needed
        string? searchResults = null;
        if (RequiresWebSearch(userInput))
        {
            _logger.LogInformation("Web search required for query: {Query}", userInput);
            searchResults = await _webSearchService.SearchAsync(userInput);
        }

        // 3. Retrieve relevant context from memory
        var relevantMemories = await GetRelevantContextAsync(userInput);

        // 4. Build contextual prompt with search results if available
        var contextualPrompt = BuildContextualPrompt(userInput, relevantMemories, searchResults);

        // 5. Get AI response
        var response = await _aiProvider.GetResponseAsync(contextualPrompt);

        // 6. Store AI response as memory
        await StoreAssistantResponseAsync(response, userInput);

        return response;
    }

    private async Task StoreUserInputAsync(string input)
    {
        var importance = _contentScorer.ScoreContent(input, "userinput");
        var tags = ExtractTagsFromContent(input);

        var memory = new MemoryItem
        {
            Content = input,
            ContentType = "userinput",
            Timestamp = DateTime.UtcNow,
            Importance = importance,
            Tags = tags,
            ConversationId = _conversationId
        };

        await _memoryStore.AddMemoryAsync(memory);
    }

    private async Task StoreAssistantResponseAsync(string response, string userInput)
    {
        var importance = _contentScorer.ScoreContent(response, "assistantresponse");
        var tags = ExtractTagsFromContent(userInput); // Use user input for context tags

        var memory = new MemoryItem
        {
            Content = response,
            ContentType = "assistantresponse",
            Timestamp = DateTime.UtcNow,
            Importance = importance,
            Tags = tags,
            ConversationId = _conversationId
        };

        await _memoryStore.AddMemoryAsync(memory);
    }

    private async Task<List<MemoryItem>> GetRelevantContextAsync(string input)
    {
        var relevantMemories = new List<MemoryItem>();

        // Get recent conversation history (last 5 exchanges)
        var recentMemories = await _memoryStore.GetRecentMemoriesAsync(10);
        relevantMemories.AddRange(recentMemories.Take(5));

        // Get semantically relevant memories (search for similar content)
        var searchResults = await _memoryStore.SearchAsync(input, 3);
        relevantMemories.AddRange(searchResults.Where(m => !relevantMemories.Any(rm => rm.Id == m.Id)));

        // Get top important memories that might be relevant
        var topMemories = await _memoryStore.GetTopMemoriesAsync(5);
        relevantMemories.AddRange(topMemories.Where(m => !relevantMemories.Any(rm => rm.Id == m.Id)));

        return relevantMemories.Take(8).ToList(); // Limit context to prevent token overflow
    }

    private string BuildContextualPrompt(string userInput, List<MemoryItem> memories, string? searchResults = null)
    {
        var contextBuilder = new List<string>();
        
        // Add search results first if available
        if (!string.IsNullOrEmpty(searchResults))
        {
            contextBuilder.Add("Here's current web search information relevant to your question:");
            contextBuilder.Add(searchResults);
            contextBuilder.Add("");
        }

        // Add conversation memory context
        if (memories.Any())
        {
            contextBuilder.Add("Here's some relevant context from our previous conversations:");
            foreach (var memory in memories.OrderBy(m => m.Timestamp))
            {
                var timeAgo = GetRelativeTime(memory.Timestamp);
                var prefix = memory.ContentType == "userinput" ? "You said" : "I responded";
                contextBuilder.Add($"- {prefix} ({timeAgo}): {memory.Content}");
            }
            contextBuilder.Add("");
        }

        contextBuilder.Add($"Current user input: {userInput}");

        return string.Join("\n", contextBuilder);
    }

    private bool RequiresWebSearch(string userInput)
    {
        var lowerInput = userInput.ToLower();
        
        // Keywords that indicate current/recent information needs
        var currentInfoKeywords = new[]
        {
            "news", "today", "latest", "current", "recent", "now", "this week", "this month", 
            "what's happening", "what happened", "breaking", "update", "today's", "recent events"
        };
        
        // Question patterns that often need current info
        var timeBasedQuestions = new[]
        {
            "what is", "what are", "what's", "tell me about", "update on", "latest on"
        };

        // Check for explicit current info requests
        if (currentInfoKeywords.Any(keyword => lowerInput.Contains(keyword)))
        {
            _logger.LogDebug("Web search triggered by current info keyword: {Input}", userInput);
            return true;
        }

        // Check for time-based questions combined with current topics
        if (timeBasedQuestions.Any(pattern => lowerInput.Contains(pattern)) && 
            (lowerInput.Contains("today") || lowerInput.Contains("now") || lowerInput.Contains("latest")))
        {
            _logger.LogDebug("Web search triggered by time-based question: {Input}", userInput);
            return true;
        }

        return false;
    }

    private List<string> ExtractTagsFromContent(string content)
    {
        var tags = new List<string>();
        var lowerContent = content.ToLower();

        // Basic keyword-based tagging
        var tagKeywords = new Dictionary<string, string[]>
        {
            ["weather"] = new[] { "weather", "temperature", "rain", "sunny", "cloudy", "forecast" },
            ["greeting"] = new[] { "hello", "hi", "hey", "good morning", "good evening" },
            ["question"] = new[] { "what", "how", "why", "when", "where", "who", "?" },
            ["preference"] = new[] { "prefer", "like", "favorite", "always", "never", "usually" },
            ["personal"] = new[] { "my name", "i am", "i live", "i work", "my job" },
            ["time"] = new[] { "time", "clock", "hour", "minute", "schedule", "calendar" },
            ["news"] = new[] { "news", "today", "latest", "current", "recent", "breaking", "update", "happening" }
        };

        foreach (var (tag, keywords) in tagKeywords)
        {
            if (keywords.Any(keyword => lowerContent.Contains(keyword)))
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    private string GetRelativeTime(DateTime timestamp)
    {
        var timeSpan = DateTime.UtcNow - timestamp;
        
        if (timeSpan.TotalMinutes < 1)
            return "just now";
        if (timeSpan.TotalHours < 1)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalDays < 1)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";
        
        return timestamp.ToString("MMM d");
    }

    public async Task<string> GetMemoryStatusAsync()
    {
        var recentMemories = await _memoryStore.GetRecentMemoriesAsync(10);
        var totalMemories = (await _memoryStore.GetTopMemoriesAsync(1000)).Count();
        
        return $"I remember {totalMemories} things from our conversations. " +
               $"Most recent: {recentMemories.FirstOrDefault()?.Content ?? "None"}";
    }
}
