using Komputa.Application.Interfaces;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Komputa.Infrastructure.Services;

/// <summary>
/// Wake word detector using Azure Speech Keyword Recognition
/// </summary>
public class KeywordSpottingWakeWordDetector : IWakeWordDetector, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeywordSpottingWakeWordDetector> _logger;
    private KeywordRecognizer? _keywordRecognizer;
    private bool _isListening;
    private string _wakeWord = "hey komputa";

    public event EventHandler? WakeWordDetected;

    public bool IsListening => _isListening;

    public string WakeWord
    {
        get => _wakeWord;
        set => _wakeWord = value;
    }

    public KeywordSpottingWakeWordDetector(
        IConfiguration configuration,
        ILogger<KeywordSpottingWakeWordDetector> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _wakeWord = _configuration["WakeWord"] ?? "hey komputa";
    }

    public async Task StartListeningAsync()
    {
        if (_isListening)
        {
            _logger.LogWarning("Already listening for wake word");
            return;
        }

        try
        {
            // Check if a keyword model file is provided
            var keywordModelPath = _configuration["AzureSpeech:KeywordModelPath"];

            if (!string.IsNullOrEmpty(keywordModelPath) && File.Exists(keywordModelPath))
            {
                // Use Azure keyword recognition with custom model
                await StartKeywordRecognitionAsync(keywordModelPath);
            }
            else
            {
                // Fallback: Use simple continuous recognition with pattern matching
                _logger.LogWarning("No keyword model found. Using pattern matching fallback. " +
                    "For better accuracy, create a keyword model at https://speech.microsoft.com/customkeyword");
                // This will be handled by the voice orchestrator using continuous recognition
            }

            _isListening = true;
            _logger.LogInformation("Started listening for wake word: '{WakeWord}'", _wakeWord);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start wake word detection");
            throw;
        }
    }

    private async Task StartKeywordRecognitionAsync(string modelPath)
    {
        var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        var model = KeywordRecognitionModel.FromFile(modelPath);

        _keywordRecognizer = new KeywordRecognizer(audioConfig);

        _keywordRecognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedKeyword)
            {
                _logger.LogInformation("Wake word detected!");
                WakeWordDetected?.Invoke(this, EventArgs.Empty);
            }
        };

        await _keywordRecognizer.RecognizeOnceAsync(model);
    }

    public Task StopListeningAsync()
    {
        if (!_isListening)
        {
            _logger.LogWarning("Not currently listening");
            return Task.CompletedTask;
        }

        try
        {
            if (_keywordRecognizer != null)
            {
                _keywordRecognizer.Dispose();
                _keywordRecognizer = null;
            }

            _isListening = false;
            _logger.LogInformation("Stopped listening for wake word");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop wake word detection");
            throw;
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _keywordRecognizer?.Dispose();
        _keywordRecognizer = null;
    }
}
