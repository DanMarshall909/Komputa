using System.Text.Json.Serialization;

namespace Komputa.Domain.Models;

public class MemoryItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("importance")]
    public double Importance { get; set; } = 0.5;
    
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
    
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }
}

public enum MemoryContentType
{
    UserInput,
    AssistantResponse,
    UserPreference,
    FactualLearning,
    ContextualFact
}
