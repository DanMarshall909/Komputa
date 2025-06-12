using Komputa.Application.Interfaces;

namespace Komputa.Infrastructure;

public class VoiceAssistantContentScorer : IContentScorer
{
    public double ScoreContent(string content, string contentType)
    {
        var baseScore = contentType.ToLower() switch
        {
            "userpreference" => 0.9,      // High importance - remember user preferences
            "factuallearning" => 0.8,     // High importance - personal facts about user
            "contextualfact" => 0.7,      // Important - contextual information
            "userinput" => 0.4,           // Lower - specific queries decay over time
            "assistantresponse" => 0.3,   // Lower - responses less important than inputs
            _ => 0.5
        };

        // Boost score for certain keywords that indicate importance
        var importanceKeywords = new[] { "remember", "prefer", "always", "never", "my name is", "i am", "i live", "i work" };
        if (importanceKeywords.Any(keyword => content.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            baseScore += 0.2;
        }

        // Cap at 1.0
        return Math.Min(baseScore, 1.0);
    }
}
