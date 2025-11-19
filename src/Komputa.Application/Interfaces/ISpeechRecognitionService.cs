namespace Komputa.Application.Interfaces;

/// <summary>
/// Service for converting speech to text
/// </summary>
public interface ISpeechRecognitionService
{
    /// <summary>
    /// Starts continuous speech recognition
    /// </summary>
    Task StartContinuousRecognitionAsync();

    /// <summary>
    /// Stops continuous speech recognition
    /// </summary>
    Task StopContinuousRecognitionAsync();

    /// <summary>
    /// Recognizes speech from microphone (one-shot)
    /// </summary>
    Task<string?> RecognizeOnceAsync();

    /// <summary>
    /// Event fired when speech is recognized
    /// </summary>
    event EventHandler<string>? SpeechRecognized;

    /// <summary>
    /// Event fired when recognition session starts
    /// </summary>
    event EventHandler? SessionStarted;

    /// <summary>
    /// Event fired when recognition session stops
    /// </summary>
    event EventHandler? SessionStopped;

    /// <summary>
    /// Gets whether the service is currently listening
    /// </summary>
    bool IsListening { get; }
}
