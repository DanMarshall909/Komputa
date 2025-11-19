namespace Komputa.Application.Interfaces;

/// <summary>
/// Service for converting text to speech
/// </summary>
public interface ISpeechSynthesisService
{
    /// <summary>
    /// Speaks the given text using the default voice
    /// </summary>
    Task SpeakAsync(string text);

    /// <summary>
    /// Speaks the given text using a specific voice
    /// </summary>
    Task SpeakAsync(string text, string voiceName);

    /// <summary>
    /// Stops current speech synthesis
    /// </summary>
    Task StopSpeakingAsync();

    /// <summary>
    /// Gets whether the service is currently speaking
    /// </summary>
    bool IsSpeaking { get; }

    /// <summary>
    /// Event fired when speech synthesis starts
    /// </summary>
    event EventHandler? SpeechStarted;

    /// <summary>
    /// Event fired when speech synthesis completes
    /// </summary>
    event EventHandler? SpeechCompleted;
}
