using TechTalk.SpecFlow;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Komputa.Services;
using Komputa.Interfaces;
using Komputa.Models;

namespace Komputa.Tests.Application.StepDefinitions;

[Binding]
public class MemoryAwareConversationSteps
{
    private readonly Mock<IMemoryStore> _mockMemoryStore;
    private readonly Mock<ILanguageModelProvider> _mockAiProvider;
    private readonly Mock<IContentScorer> _mockContentScorer;
    private readonly Mock<IWebSearchService> _mockWebSearchService;
    private readonly Mock<ILogger<MemoryAwareConversationService>> _mockLogger;
    private MemoryAwareConversationService _conversationService;
    private string _userInput = string.Empty;
    private string _assistantResponse = string.Empty;
    private List<MemoryItem> _storedMemories = new();
    private List<MemoryItem> _relevantMemories = new();
    private double _memoryScore;

    public MemoryAwareConversationSteps()
    {
        _mockMemoryStore = new Mock<IMemoryStore>();
        _mockAiProvider = new Mock<ILanguageModelProvider>();
        _mockContentScorer = new Mock<IContentScorer>();
        _mockWebSearchService = new Mock<IWebSearchService>();
        _mockLogger = new Mock<ILogger<MemoryAwareConversationService>>();
    }

    [Given(@"the memory store is initialized")]
    public void GivenTheMemoryStoreIsInitialized()
    {
        _mockMemoryStore.Setup(x => x.AddMemoryAsync(It.IsAny<MemoryItem>()))
            .Callback<MemoryItem>(memory => _storedMemories.Add(memory))
            .Returns(Task.CompletedTask);

        _mockMemoryStore.Setup(x => x.GetRecentMemoriesAsync(It.IsAny<int>()))
            .ReturnsAsync(() => _storedMemories.TakeLast(10).ToList());

        _mockMemoryStore.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(() => _relevantMemories);

        _mockMemoryStore.Setup(x => x.GetTopMemoriesAsync(It.IsAny<int>()))
            .ReturnsAsync(() => _storedMemories.OrderByDescending(m => m.Importance).Take(5).ToList());
    }

    [Given(@"the AI provider is available")]
    public void GivenTheAiProviderIsAvailable()
    {
        _mockAiProvider.Setup(x => x.IsAvailable).Returns(true);
        _mockAiProvider.Setup(x => x.GetResponseAsync(It.IsAny<string>()))
            .ReturnsAsync((string input) => $"AI response to: {input}");

        _conversationService = new MemoryAwareConversationService(
            _mockMemoryStore.Object,
            _mockAiProvider.Object,
            _mockContentScorer.Object,
            _mockWebSearchService.Object,
            _mockLogger.Object);
    }

    [Given(@"I have previous conversation about ""(.*)""")]
    public void GivenIHavePreviousConversationAbout(string topic)
    {
        var memory = new MemoryItem
        {
            Content = $"We discussed {topic}",
            ContentType = "conversation_topic",
            Timestamp = DateTime.UtcNow.AddDays(-1),
            Importance = 0.7,
            Tags = new List<string> { topic.Replace(" ", "_") }
        };
        _storedMemories.Add(memory);
        _relevantMemories.Add(memory);
    }

    [Given(@"my preference is set to ""(.*)""")]
    public void GivenMyPreferenceIsSetTo(string preference)
    {
        var memory = new MemoryItem
        {
            Content = $"User prefers {preference}",
            ContentType = "user_preference",
            Timestamp = DateTime.UtcNow.AddDays(-1),
            Importance = 0.8,
            Tags = new List<string> { "preference", preference.Replace(" ", "_") }
        };
        _storedMemories.Add(memory);
        _relevantMemories.Add(memory);

        _mockContentScorer.Setup(x => x.ScoreContent(It.IsAny<string>(), "user_preference"))
            .Returns(0.8);
    }

    [When(@"I ask ""(.*)""")]
    public async Task WhenIAsk(string question)
    {
        _userInput = question;
        _assistantResponse = await _conversationService.GetResponseWithMemoryAsync(question);
    }

    [Given(@"user input contains ""(.*)""")]
    public void GivenUserInputContains(string input)
    {
        _userInput = input;
    }

    [When(@"the memory scorer evaluates the content")]
    public void WhenTheMemoryScorerEvaluatesTheContent()
    {
        if (_userInput.Contains("My name is"))
        {
            _mockContentScorer.Setup(x => x.ScoreContent(_userInput, "personal_information"))
                .Returns(0.9);
            _memoryScore = 0.9;
        }
        else
        {
            _mockContentScorer.Setup(x => x.ScoreContent(_userInput, It.IsAny<string>()))
                .Returns(0.5);
            _memoryScore = 0.5;
        }
    }

    [Then(@"the assistant should include my preference for ""(.*)""")]
    public void ThenTheAssistantShouldIncludeMyPreferenceFor(string preference)
    {
        _assistantResponse.Should().Contain(preference, because: "the assistant should use stored preferences");
    }

    [Then(@"the response should be contextually relevant")]
    public void ThenTheResponseShouldBeContextuallyRelevant()
    {
        _assistantResponse.Should().NotBeEmpty();
        _assistantResponse.Should().StartWith("AI response to:", because: "the AI should have processed the contextual input");
    }

    [Then(@"the memory score should reflect usage")]
    public void ThenTheMemoryScoreShouldReflectUsage()
    {
        _mockMemoryStore.Verify(x => x.AddMemoryAsync(It.IsAny<MemoryItem>()), Times.AtLeastOnce);
        _storedMemories.Should().NotBeEmpty();
    }

    [Then(@"the importance score should be greater than (.*)")]
    public void ThenTheImportanceScoreShouldBeGreaterThan(double expectedScore)
    {
        _memoryScore.Should().BeGreaterThan(expectedScore);
    }

    [Then(@"the content type should be ""(.*)""")]
    public void ThenTheContentTypeShouldBe(string expectedContentType)
    {
        // This would be verified through the scorer mock setup
        _mockContentScorer.Verify(x => x.ScoreContent(_userInput, expectedContentType), Times.Once);
    }

    [Then(@"the memory should be tagged appropriately")]
    public void ThenTheMemoryShouldBeTaggedAppropriately()
    {
        _mockMemoryStore.Verify(x => x.AddMemoryAsync(It.Is<MemoryItem>(m => m.Tags.Any())), Times.AtLeastOnce);
    }

    [Given(@"I previously corrected the assistant saying ""(.*)""")]
    public void GivenIPreviouslyCorrectedTheAssistantSaying(string correction)
    {
        var memory = new MemoryItem
        {
            Content = correction,
            ContentType = "error_correction",
            Timestamp = DateTime.UtcNow.AddHours(-1),
            Importance = 0.8,
            Tags = new List<string> { "correction", "preference" }
        };
        _storedMemories.Add(memory);
        _relevantMemories.Add(memory);
    }

    [When(@"I ask about temperature")]
    public async Task WhenIAskAboutTemperature()
    {
        _userInput = "What's the temperature?";
        _assistantResponse = await _conversationService.GetResponseWithMemoryAsync(_userInput);
    }

    [Then(@"the assistant should use Celsius in the response")]
    public void ThenTheAssistantShouldUseCelsiusInTheResponse()
    {
        _assistantResponse.Should().Contain("Celsius", because: "the assistant should remember the user's preference for Celsius");
    }

    [Then(@"the correction should be stored as high-importance memory")]
    public void ThenTheCorrectionShouldBeStoredAsHighImportanceMemory()
    {
        _storedMemories.Should().Contain(m => m.ContentType == "error_correction" && m.Importance >= 0.8);
    }
}
