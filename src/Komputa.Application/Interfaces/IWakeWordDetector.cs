namespace Komputa.Application.Interfaces;

/// <summary>
/// Service for detecting wake words like "hey komputa"
/// </summary>
public interface IWakeWordDetector
{
    /// <summary>
    /// Starts listening for the wake word
    /// </summary>
    Task StartListeningAsync();

    /// <summary>
    /// Stops listening for the wake word
    /// </summary>
    Task StopListeningAsync();

    /// <summary>
    /// Event fired when wake word is detected
    /// </summary>
    event EventHandler? WakeWordDetected;

    /// <summary>
    /// Gets whether the detector is currently listening
    /// </summary>
    bool IsListening { get; }

    /// <summary>
    /// Gets or sets the wake word phrase (e.g., "hey komputa")
    /// </summary>
    string WakeWord { get; set; }
}
