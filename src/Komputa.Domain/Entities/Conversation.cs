using Komputa.Domain.ValueObjects;

namespace Komputa.Domain.Entities;

/// <summary>
/// Aggregate root managing memory collection with business rules enforcement
/// </summary>
public class Conversation
{
    private readonly List<MemoryItem> _memories;
    private const int MaxContextMemories = 8;
    private const int MaxRecentMemories = 5;
    private const int MaxSemanticResults = 3;
    private const int MaxImportantMemories = 5;

    public ConversationId Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public IReadOnlyList<MemoryItem> Memories => _memories.AsReadOnly();
    public int MemoryCount => _memories.Count;

    // For EF Core or serialization
    private Conversation()
    {
        _memories = new List<MemoryItem>();
    }

    public Conversation(ConversationId? conversationId = null)
    {
        Id = conversationId ?? ConversationId.NewId();
        CreatedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
        _memories = new List<MemoryItem>();
    }

    /// <summary>
    /// Add memory to the conversation with business rule validation
    /// </summary>
    public void AddMemory(MemoryItem memory)
    {
        if (memory == null)
            throw new ArgumentNullException(nameof(memory));

        // Enforce conversation ID consistency invariant
        if (!memory.BelongsToConversation(Id))
            throw new InvalidOperationException($"Memory belongs to conversation {memory.ConversationId} but this is conversation {Id}");

        // Check for duplicates
        if (_memories.Any(m => m.Id.Equals(memory.Id, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Memory with ID {memory.Id} already exists in conversation");

        _memories.Add(memory);
        LastActivityAt = DateTime.UtcNow;

        // TODO: Raise domain event - MemoryAddedEvent
    }

    /// <summary>
    /// Add memory from user input with automatic processing
    /// </summary>
    public MemoryItem AddUserInput(UserInput userInput)
    {
        if (userInput == null)
            throw new ArgumentNullException(nameof(userInput));

        var memory = MemoryItem.FromUserInput(userInput, Id);
        AddMemory(memory);
        return memory;
    }

    /// <summary>
    /// Add memory from assistant response
    /// </summary>
    public MemoryItem AddAssistantResponse(string response, List<string>? contextTags = null)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new ArgumentException("Assistant response cannot be null or empty", nameof(response));

        var memory = MemoryItem.FromAssistantResponse(response, Id, contextTags);
        AddMemory(memory);
        return memory;
    }

    /// <summary>
    /// Get relevant context for AI conversation following business rules
    /// </summary>
    public List<MemoryItem> GetRelevantContext(UserInput input, DateTime? currentTime = null)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var now = currentTime ?? DateTime.UtcNow;
        var relevantMemories = new List<MemoryItem>();

        // 1. Get recent conversation history (last 5 exchanges)
        var recentMemories = GetRecentMemories(MaxRecentMemories * 2) // Get more to account for user+assistant pairs
            .Take(MaxRecentMemories)
            .ToList();
        relevantMemories.AddRange(recentMemories);

        // 2. Get semantically relevant memories (search for similar content)
        var semanticMatches = GetSemanticMatches(input, MaxSemanticResults)
            .Where(m => !relevantMemories.Any(rm => rm.Id == m.Id))
            .ToList();
        relevantMemories.AddRange(semanticMatches);

        // 3. Get top important memories that might be relevant
        var importantMemories = GetTopImportantMemories(MaxImportantMemories, now)
            .Where(m => !relevantMemories.Any(rm => rm.Id == m.Id))
            .ToList();
        relevantMemories.AddRange(importantMemories);

        // 4. Limit context to prevent token overflow
        return relevantMemories
            .Take(MaxContextMemories)
            .OrderBy(m => m.Timestamp) // Chronological order for context
            .ToList();
    }

    /// <summary>
    /// Get recent memories from conversation
    /// </summary>
    public List<MemoryItem> GetRecentMemories(int count = 10)
    {
        return _memories
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Search memories semantically based on input
    /// </summary>
    public List<MemoryItem> GetSemanticMatches(UserInput input, int maxResults = 3)
    {
        // Simple implementation - in real system would use vector search
        return _memories
            .Where(m => m.IsRelevantTo(input))
            .OrderByDescending(m => m.ImportanceScore.Value)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Get top important memories with decay applied
    /// </summary>
    public List<MemoryItem> GetTopImportantMemories(int count = 5, DateTime? currentTime = null)
    {
        var now = currentTime ?? DateTime.UtcNow;
        
        return _memories
            .Select(m => new { Memory = m, DecayedScore = m.CalculateDecayedImportance(now) })
            .OrderByDescending(x => x.DecayedScore.Value)
            .Take(count)
            .Select(x => x.Memory)
            .ToList();
    }

    /// <summary>
    /// Cleanup old memories based on retention rules
    /// </summary>
    public int CleanupOldMemories(DateTime cutoffDate)
    {
        var memoriesToRemove = _memories
            .Where(m => !m.ContentType.IsLongTermRetention()) // Keep user preferences and personal facts
            .Where(m => m.Timestamp < cutoffDate)
            .Where(m => m.ImportanceScore.Value < 0.3) // Keep high-importance memories
            .ToList();

        foreach (var memory in memoriesToRemove)
        {
            _memories.Remove(memory);
            // TODO: Raise domain event - MemoryRemovedEvent
        }

        if (memoriesToRemove.Any())
        {
            LastActivityAt = DateTime.UtcNow;
        }

        return memoriesToRemove.Count;
    }

    /// <summary>
    /// Boost memory scores for recent usage
    /// </summary>
    public void BoostMemoryUsage(IEnumerable<string> memoryIds, double boostFactor = 0.1)
    {
        if (memoryIds == null)
            return;

        var idsToBoost = memoryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var boostedCount = 0;

        foreach (var memory in _memories.Where(m => idsToBoost.Contains(m.Id)))
        {
            memory.BoostForUsage(boostFactor);
            boostedCount++;
        }

        if (boostedCount > 0)
        {
            LastActivityAt = DateTime.UtcNow;
            // TODO: Raise domain event - MemoryUsageBoostedEvent
        }
    }

    /// <summary>
    /// Get memory by ID
    /// </summary>
    public MemoryItem? GetMemoryById(string memoryId)
    {
        if (string.IsNullOrWhiteSpace(memoryId))
            return null;

        return _memories.FirstOrDefault(m => m.Id.Equals(memoryId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Search memories by content
    /// </summary>
    public List<MemoryItem> SearchMemories(string searchTerm, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<MemoryItem>();

        var lowerSearchTerm = searchTerm.ToLower();
        
        return _memories
            .Where(m => m.Content.ToLower().Contains(lowerSearchTerm) || 
                       m.Tags.Any(tag => tag.Contains(lowerSearchTerm)))
            .OrderByDescending(m => m.ImportanceScore.Value)
            .ThenByDescending(m => m.Timestamp)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Get conversation statistics
    /// </summary>
    public ConversationStats GetStats(DateTime? currentTime = null)
    {
        var now = currentTime ?? DateTime.UtcNow;
        
        return new ConversationStats
        {
            TotalMemories = _memories.Count,
            HighPriorityMemories = _memories.Count(m => m.IsHighPriority()),
            AverageImportance = _memories.Any() ? _memories.Average(m => m.ImportanceScore.Value) : 0,
            OldestMemory = _memories.MinBy(m => m.Timestamp)?.Timestamp,
            NewestMemory = _memories.MaxBy(m => m.Timestamp)?.Timestamp,
            ConversationAge = now - CreatedAt,
            LastActivity = now - LastActivityAt
        };
    }

    /// <summary>
    /// Build contextual prompt with memories and search results
    /// </summary>
    public string BuildContextualPrompt(UserInput userInput, List<MemoryItem> relevantMemories, string? searchResults = null)
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
        if (relevantMemories.Any())
        {
            contextBuilder.Add("Here's some relevant context from our previous conversations:");
            var now = DateTime.UtcNow;
            foreach (var memory in relevantMemories.OrderBy(m => m.Timestamp))
            {
                var timeAgo = memory.GetRelativeTime(now);
                var prefix = memory.ContentType.Value == "userinput" ? "You said" : "I responded";
                contextBuilder.Add($"- {prefix} ({timeAgo}): {memory.Content}");
            }
            contextBuilder.Add("");
        }

        contextBuilder.Add($"Current user input: {userInput}");

        return string.Join("\n", contextBuilder);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Conversation other)
            return false;
            
        return Id.Value.Equals(other.Id.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return $"Conversation[{Id.Value[..8]}]: {_memories.Count} memories, Created: {CreatedAt:MMM d}";
    }
}

/// <summary>
/// Value object containing conversation statistics
/// </summary>
public record ConversationStats
{
    public int TotalMemories { get; init; }
    public int HighPriorityMemories { get; init; }
    public double AverageImportance { get; init; }
    public DateTime? OldestMemory { get; init; }
    public DateTime? NewestMemory { get; init; }
    public TimeSpan ConversationAge { get; init; }
    public TimeSpan LastActivity { get; init; }
}
