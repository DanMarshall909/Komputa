using Komputa.Interfaces;
using Komputa.Models;

namespace Komputa.Services;

public class MemoryAwareConversationService
{
    private readonly IMemoryStore _memoryStore;
    private readonly ILanguageModelProvider _aiProvider;
    private readonly IContentScorer _contentScorer;
    private readonly string _conversationId;

    public MemoryAwareConversationService(
        IMemoryStore memoryStore,
        ILanguageModelProvider aiProvider,
        IContentScorer contentScorer)
    {
        _memoryStore = memoryStore;
        _aiProvider = aiProvider;
        _contentScorer = contentScorer;
        _conversationId = Guid.NewGuid().ToString();
    }

    public async Task<string> GetResponseWithMemoryAsync(string userInput)
    {
        // 1. Store user input as memory item
        await StoreUserInputAsync(userInput);

        // 2. Retrieve relevant context from memory
        var relevantMemories = await GetRelevantContextAsync(userInput);

        // 3. Build contextual prompt
        var contextualPrompt = BuildContextualPrompt(userInput, relevantMemories);

        // 4. Get AI response
        var response = await _aiProvider.GetResponseAsync(contextualPrompt);

        // 5. Store AI response as memory
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

    private string BuildContextualPrompt(string userInput, List<MemoryItem> memories)
    {
        if (!memories.Any())
        {
            return userInput;
        }

        var contextBuilder = new List<string>();
        contextBuilder.Add("Here's some relevant context from our previous conversations:");

        foreach (var memory in memories.OrderBy(m => m.Timestamp))
        {
            var timeAgo = GetRelativeTime(memory.Timestamp);
            var prefix = memory.ContentType == "userinput" ? "You said" : "I responded";
            contextBuilder.Add($"- {prefix} ({timeAgo}): {memory.Content}");
        }

        contextBuilder.Add("");
        contextBuilder.Add($"Current user input: {userInput}");

        return string.Join("\n", contextBuilder);
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
            ["time"] = new[] { "time", "clock", "hour", "minute", "schedule", "calendar" }
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
