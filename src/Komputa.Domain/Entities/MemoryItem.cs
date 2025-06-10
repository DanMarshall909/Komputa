using Komputa.Domain.ValueObjects;

namespace Komputa.Domain.Entities;

/// <summary>
/// Rich domain entity representing a memory item with business behavior
/// </summary>
public class MemoryItem
{
    private readonly List<string> _tags;
    
    public string Id { get; private set; }
    public string Content { get; private set; }
    public ContentType ContentType { get; private set; }
    public DateTime Timestamp { get; private set; }
    public MemoryScore ImportanceScore { get; private set; }
    public ConversationId ConversationId { get; private set; }
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    // For EF Core or serialization
    private MemoryItem() 
    {
        _tags = new List<string>();
    }

    public MemoryItem(
        string content,
        ContentType contentType,
        ConversationId conversationId,
        DateTime? timestamp = null,
        MemoryScore? importanceScore = null,
        IEnumerable<string>? tags = null,
        string? id = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Memory content cannot be null or empty", nameof(content));

        Id = id ?? Guid.NewGuid().ToString();
        Content = content.Trim();
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        ConversationId = conversationId ?? throw new ArgumentNullException(nameof(conversationId));
        Timestamp = timestamp ?? DateTime.UtcNow;
        ImportanceScore = importanceScore ?? MemoryScore.Create(contentType.BaseImportanceScore);
        _tags = tags?.ToList() ?? new List<string>();

        // Apply automatic scoring enhancements
        ApplyAutomaticScoring();
    }

    /// <summary>
    /// Create memory from user input with automatic content analysis
    /// </summary>
    public static MemoryItem FromUserInput(UserInput userInput, ConversationId conversationId)
    {
        var contentType = ContentType.DetermineFromContent(userInput.Value, isUserInput: true);
        var tags = userInput.ExtractTags();
        
        return new MemoryItem(
            content: userInput.Value,
            contentType: contentType,
            conversationId: conversationId,
            tags: tags);
    }

    /// <summary>
    /// Create memory from assistant response
    /// </summary>
    public static MemoryItem FromAssistantResponse(string response, ConversationId conversationId, List<string>? contextTags = null)
    {
        var contentType = ContentType.AssistantResponse;
        
        return new MemoryItem(
            content: response,
            contentType: contentType,
            conversationId: conversationId,
            tags: contextTags ?? new List<string>());
    }

    /// <summary>
    /// Calculate current importance with time-based decay applied
    /// </summary>
    public MemoryScore CalculateDecayedImportance(DateTime currentTime)
    {
        return ImportanceScore.ApplyTimeDecay(Timestamp, currentTime);
    }

    /// <summary>
    /// Boost memory score for recent usage
    /// </summary>
    public void BoostForUsage(double boostFactor = 0.1)
    {
        ImportanceScore = ImportanceScore.ApplyUsageBoost(boostFactor);
    }

    /// <summary>
    /// Add tags to the memory item
    /// </summary>
    public void AddTags(params string[] newTags)
    {
        if (newTags == null || newTags.Length == 0)
            return;

        foreach (var tag in newTags.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            var cleanTag = tag.Trim().ToLower();
            if (!_tags.Contains(cleanTag))
            {
                _tags.Add(cleanTag);
            }
        }
    }

    /// <summary>
    /// Remove tags from the memory item
    /// </summary>
    public void RemoveTags(params string[] tagsToRemove)
    {
        if (tagsToRemove == null || tagsToRemove.Length == 0)
            return;

        foreach (var tag in tagsToRemove.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            _tags.Remove(tag.Trim().ToLower());
        }
    }

    /// <summary>
    /// Check if memory is relevant to given input based on tags and content
    /// </summary>
    public bool IsRelevantTo(UserInput input)
    {
        var inputTags = input.ExtractTags();
        var hasCommonTags = inputTags.Any(tag => _tags.Contains(tag));
        
        // Also check for content similarity (basic keyword matching)
        var inputWords = input.Value.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var contentWords = Content.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var hasCommonWords = inputWords.Intersect(contentWords).Any();

        return hasCommonTags || hasCommonWords;
    }

    /// <summary>
    /// Check if this memory should be prioritized in context selection
    /// </summary>
    public bool IsHighPriority()
    {
        return ContentType.IsHighPriority() || ImportanceScore.Value >= 0.8;
    }

    /// <summary>
    /// Get relative time description for UI display
    /// </summary>
    public string GetRelativeTime(DateTime currentTime)
    {
        var timeSpan = currentTime - Timestamp;
        
        if (timeSpan.TotalMinutes < 1)
            return "just now";
        if (timeSpan.TotalHours < 1)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalDays < 1)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";
        
        return Timestamp.ToString("MMM d");
    }

    /// <summary>
    /// Check if this memory belongs to the same conversation
    /// </summary>
    public bool BelongsToConversation(ConversationId conversationId)
    {
        return ConversationId.Value.Equals(conversationId.Value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Update importance score (for external scoring algorithms)
    /// </summary>
    public void UpdateImportanceScore(MemoryScore newScore)
    {
        ImportanceScore = newScore ?? throw new ArgumentNullException(nameof(newScore));
    }

    /// <summary>
    /// Create a copy of this memory for a different conversation
    /// </summary>
    public MemoryItem CopyToConversation(ConversationId newConversationId)
    {
        return new MemoryItem(
            content: Content,
            contentType: ContentType,
            conversationId: newConversationId,
            timestamp: Timestamp,
            importanceScore: ImportanceScore,
            tags: _tags.ToList());
    }

    private void ApplyAutomaticScoring()
    {
        // Apply importance keyword boost if content contains them
        var userInput = UserInput.Create(Content);
        if (userInput.ContainsImportanceKeywords())
        {
            ImportanceScore = ImportanceScore.ApplyKeywordBoost();
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is not MemoryItem other)
            return false;
            
        return Id.Equals(other.Id, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return $"Memory[{Id[..8]}]: {Content[..Math.Min(50, Content.Length)]}..." +
               $" (Score: {ImportanceScore}, Type: {ContentType})";
    }
}
