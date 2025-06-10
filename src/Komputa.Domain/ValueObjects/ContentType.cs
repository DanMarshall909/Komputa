namespace Komputa.Domain.ValueObjects;

/// <summary>
/// Enum with business rules for each content type
/// </summary>
public record ContentType
{
    public string Value { get; }
    public double BaseImportanceScore { get; }

    private ContentType(string value, double baseScore)
    {
        Value = value;
        BaseImportanceScore = baseScore;
    }

    // Predefined content types with their business rules
    public static readonly ContentType UserPreference = new("userpreference", 0.9);
    public static readonly ContentType FactualLearning = new("factuallearning", 0.8);
    public static readonly ContentType ContextualFact = new("contextualfact", 0.7);
    public static readonly ContentType UserInput = new("userinput", 0.4);
    public static readonly ContentType AssistantResponse = new("assistantresponse", 0.3);
    public static readonly ContentType Default = new("default", 0.5);

    private static readonly Dictionary<string, ContentType> _types = new()
    {
        ["userpreference"] = UserPreference,
        ["factuallearning"] = FactualLearning,
        ["contextualfact"] = ContextualFact,
        ["userinput"] = UserInput,
        ["assistantresponse"] = AssistantResponse,
        ["default"] = Default
    };

    public static ContentType FromString(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return Default;

        var lowerType = contentType.ToLower();
        return _types.TryGetValue(lowerType, out var type) ? type : Default;
    }

    /// <summary>
    /// Determine content type based on context and content analysis
    /// </summary>
    public static ContentType DetermineFromContent(string content, bool isUserInput = true)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Default;

        var lowerContent = content.ToLower();

        // Check for user preferences
        if (lowerContent.Contains("prefer") || lowerContent.Contains("like") || 
            lowerContent.Contains("always") || lowerContent.Contains("never") ||
            lowerContent.Contains("favorite"))
        {
            return UserPreference;
        }

        // Check for personal facts
        if (lowerContent.Contains("my name is") || lowerContent.Contains("i am") ||
            lowerContent.Contains("i live") || lowerContent.Contains("i work") ||
            lowerContent.Contains("my job"))
        {
            return FactualLearning;
        }

        // Check for contextual facts (specific information)
        if (lowerContent.Contains("remember") || lowerContent.Contains("important") ||
            lowerContent.Contains("note that"))
        {
            return ContextualFact;
        }

        // Default classification based on source
        return isUserInput ? UserInput : AssistantResponse;
    }

    /// <summary>
    /// Check if this content type should have higher retention priority
    /// </summary>
    public bool IsHighPriority() => BaseImportanceScore >= 0.7;

    /// <summary>
    /// Check if this content type is suitable for long-term storage
    /// </summary>
    public bool IsLongTermRetention() => this == UserPreference || this == FactualLearning;

    public static implicit operator string(ContentType contentType) => contentType.Value;
    
    public override string ToString() => Value;
}
