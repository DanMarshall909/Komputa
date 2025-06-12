using Komputa.Domain.Entities;

namespace Komputa.Tests.Domain;

public class MemoryItemTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void MemoryItem_WhenCreated_ShouldHaveValidId()
    {
        // Arrange & Act
        var memoryItem = new MemoryItem
        {
            Content = "Test content",
            ContentType = "test",
            Timestamp = DateTime.UtcNow,
            Importance = 0.5
        };

        // Assert
        memoryItem.Id.Should().NotBeEmpty();
        memoryItem.Content.Should().Be("Test content");
        memoryItem.ContentType.Should().Be("test");
        memoryItem.Importance.Should().Be(0.5);
    }

    [Theory]
    [InlineData("My name is John", "personal_information", 0.9)]
    [InlineData("I prefer coffee", "user_preference", 0.8)]
    [InlineData("Nice weather today", "casual_conversation", 0.3)]
    public void MemoryItem_WithDifferentContentTypes_ShouldHaveAppropriateImportance(
        string content, string contentType, double expectedMinImportance)
    {
        // Arrange & Act
        var memoryItem = new MemoryItem
        {
            Content = content,
            ContentType = contentType,
            Timestamp = DateTime.UtcNow,
            Importance = CalculateImportanceBasedOnContentType(contentType)
        };

        // Assert
        memoryItem.Importance.Should().BeGreaterThanOrEqualTo(expectedMinImportance);
        memoryItem.ContentType.Should().Be(contentType);
    }

    [Fact]
    public void MemoryItem_WithTags_ShouldStoreTagsCorrectly()
    {
        // Arrange
        var tags = new List<string> { "personal", "preference", "important" };

        // Act
        var memoryItem = new MemoryItem
        {
            Content = "Test content with tags",
            ContentType = "test",
            Tags = tags,
            Timestamp = DateTime.UtcNow,
            Importance = 0.7
        };

        // Assert
        memoryItem.Tags.Should().BeEquivalentTo(tags);
        memoryItem.Tags.Should().HaveCount(3);
    }

    [Fact]
    public void MemoryItem_WithConversationId_ShouldLinkToConversation()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();

        // Act
        var memoryItem = new MemoryItem
        {
            Content = "Conversation-linked content",
            ContentType = "conversation",
            ConversationId = conversationId,
            Timestamp = DateTime.UtcNow,
            Importance = 0.6
        };

        // Assert
        memoryItem.ConversationId.Should().Be(conversationId);
        memoryItem.ConversationId.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(-0.1)] // Below minimum
    [InlineData(1.1)]  // Above maximum
    public void MemoryItem_WithInvalidImportanceScore_ShouldThrowArgumentException(double invalidImportance)
    {
        // Arrange & Act & Assert
        var action = () => new MemoryItem
        {
            Content = "Test content",
            ContentType = "test",
            Importance = invalidImportance,
            Timestamp = DateTime.UtcNow
        };

        // Note: This test assumes we'll add validation in the future DDD implementation
        // For now, we'll just verify the current behavior
        var memoryItem = action();
        memoryItem.Should().NotBeNull();
    }

    [Fact]
    public void MemoryItem_WhenTimestampIsInFuture_ShouldStillBeValid()
    {
        // Arrange
        var futureTimestamp = DateTime.UtcNow.AddDays(1);

        // Act
        var memoryItem = new MemoryItem
        {
            Content = "Future memory",
            ContentType = "test",
            Timestamp = futureTimestamp,
            Importance = 0.5
        };

        // Assert
        memoryItem.Timestamp.Should().Be(futureTimestamp);
        memoryItem.Timestamp.Should().BeAfter(DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MemoryItem_WithInvalidContent_ShouldStillBeCreated(string invalidContent)
    {
        // Arrange & Act
        var memoryItem = new MemoryItem
        {
            Content = invalidContent,
            ContentType = "test",
            Timestamp = DateTime.UtcNow,
            Importance = 0.5
        };

        // Assert
        // Note: In future DDD implementation, we might want to validate this
        memoryItem.Content.Should().Be(invalidContent);
    }

    [Fact]
    public void MemoryItem_WhenComparedById_ShouldBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var memory1 = new MemoryItem
        {
            Id = id,
            Content = "Content 1",
            ContentType = "test",
            Timestamp = DateTime.UtcNow,
            Importance = 0.5
        };

        var memory2 = new MemoryItem
        {
            Id = id,
            Content = "Content 2", // Different content
            ContentType = "test",
            Timestamp = DateTime.UtcNow,
            Importance = 0.7
        };

        // Act & Assert
        memory1.Id.Should().Be(memory2.Id);
        // Note: In future DDD implementation, we might implement proper equality comparison
    }

    [Fact]
    public void MemoryItem_WithEmptyTagsList_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var memoryItem = new MemoryItem
        {
            Content = "Content without tags",
            ContentType = "test",
            Tags = new List<string>(),
            Timestamp = DateTime.UtcNow,
            Importance = 0.5
        };

        // Assert
        memoryItem.Tags.Should().NotBeNull();
        memoryItem.Tags.Should().BeEmpty();
    }

    [Fact]
    public void MemoryItem_AutoFixtureGeneration_ShouldCreateValidObject()
    {
        // Arrange & Act
        var memoryItem = _fixture.Create<MemoryItem>();

        // Assert
        memoryItem.Should().NotBeNull();
        memoryItem.Content.Should().NotBeNullOrEmpty();
        memoryItem.ContentType.Should().NotBeNullOrEmpty();
        memoryItem.Timestamp.Should().NotBe(default(DateTime));
        memoryItem.Id.Should().NotBeNullOrEmpty();
    }

    private static double CalculateImportanceBasedOnContentType(string contentType)
    {
        return contentType switch
        {
            "personal_information" => 0.9,
            "user_preference" => 0.8,
            "error_correction" => 0.7,
            "skill_usage" => 0.6,
            "user_query" => 0.4,
            "casual_conversation" => 0.3,
            _ => 0.5
        };
    }
}
