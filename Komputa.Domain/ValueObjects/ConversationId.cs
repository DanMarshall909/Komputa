namespace Komputa.Domain.ValueObjects;

/// <summary>
/// Strong-typed conversation identifier preventing mix-ups
/// </summary>
public record ConversationId
{
    public string Value { get; }

    private ConversationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Conversation ID cannot be null or empty", nameof(value));
            
        if (!Guid.TryParse(value, out _))
            throw new ArgumentException("Conversation ID must be a valid GUID", nameof(value));
            
        Value = value;
    }

    public static ConversationId Create(string value) => new(value);
    
    public static ConversationId NewId() => new(Guid.NewGuid().ToString());

    public static implicit operator string(ConversationId conversationId) => conversationId.Value;
    
    public override string ToString() => Value;
}
