namespace Komputa.Domain.ValueObjects;

/// <summary>
/// Immutable user input with validation rules
/// </summary>
public record UserInput
{
    public string Value { get; }
    
    private UserInput(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("User input cannot be null or empty", nameof(value));
            
        if (value.Length > 10000) // Reasonable limit for conversation input
            throw new ArgumentException("User input cannot exceed 10,000 characters", nameof(value));
            
        Value = value.Trim();
    }

    public static UserInput Create(string value) => new(value);

    /// <summary>
    /// Check if input requires web search based on content analysis
    /// </summary>
    public bool RequiresWebSearch()
    {
        var lowerInput = Value.ToLower();
        
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
            return true;

        // Check for time-based questions combined with current topics
        if (timeBasedQuestions.Any(pattern => lowerInput.Contains(pattern)) && 
            (lowerInput.Contains("today") || lowerInput.Contains("now") || lowerInput.Contains("latest")))
            return true;

        return false;
    }

    /// <summary>
    /// Extract tags from the input content
    /// </summary>
    public List<string> ExtractTags()
    {
        var tags = new List<string>();
        var lowerContent = Value.ToLower();

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

    /// <summary>
    /// Check if input contains importance keywords that boost memory score
    /// </summary>
    public bool ContainsImportanceKeywords()
    {
        var lowerInput = Value.ToLower();
        var importanceKeywords = new[] { "remember", "prefer", "always", "never", "my name is", "i am", "i live", "i work" };
        return importanceKeywords.Any(keyword => lowerInput.Contains(keyword));
    }

    public static implicit operator string(UserInput userInput) => userInput.Value;
    
    public override string ToString() => Value;
}
