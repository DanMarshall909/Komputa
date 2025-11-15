using Komputa.Application.Interfaces;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Komputa.Infrastructure.Services;

public class AzureSpeechRecognitionService : ISpeechRecognitionService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureSpeechRecognitionService> _logger;
    private SpeechRecognizer? _recognizer;
    private bool _isListening;

    public event EventHandler<string>? SpeechRecognized;
    public event EventHandler? SessionStarted;
    public event EventHandler? SessionStopped;

    public bool IsListening => _isListening;

    public AzureSpeechRecognitionService(
        IConfiguration configuration,
        ILogger<AzureSpeechRecognitionService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private SpeechRecognizer GetOrCreateRecognizer()
    {
        if (_recognizer != null)
            return _recognizer;

        var subscriptionKey = _configuration["AzureSpeech:SubscriptionKey"];
        var region = _configuration["AzureSpeech:Region"];

        if (string.IsNullOrEmpty(subscriptionKey) || string.IsNullOrEmpty(region))
        {
            throw new InvalidOperationException(
                "Azure Speech configuration is missing. Please set AzureSpeech:SubscriptionKey and AzureSpeech:Region in user secrets or appsettings.json");
        }

        var config = SpeechConfig.FromSubscription(subscriptionKey, region);

        // Set language and other properties
        config.SpeechRecognitionLanguage = _configuration["AzureSpeech:Language"] ?? "en-US";

        var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        _recognizer = new SpeechRecognizer(config, audioConfig);

        // Wire up events
        _recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrWhiteSpace(e.Result.Text))
            {
                _logger.LogInformation("Recognized: {Text}", e.Result.Text);
                SpeechRecognized?.Invoke(this, e.Result.Text);
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                _logger.LogDebug("No speech could be recognized");
            }
        };

        _recognizer.SessionStarted += (s, e) =>
        {
            _logger.LogInformation("Speech recognition session started");
            SessionStarted?.Invoke(this, EventArgs.Empty);
        };

        _recognizer.SessionStopped += (s, e) =>
        {
            _logger.LogInformation("Speech recognition session stopped");
            _isListening = false;
            SessionStopped?.Invoke(this, EventArgs.Empty);
        };

        _recognizer.Canceled += (s, e) =>
        {
            _logger.LogWarning("Speech recognition canceled: {Reason}", e.Reason);
            if (e.Reason == CancellationReason.Error)
            {
                _logger.LogError("Error: {ErrorCode} - {ErrorDetails}", e.ErrorCode, e.ErrorDetails);
            }
            _isListening = false;
        };

        return _recognizer;
    }

    public async Task StartContinuousRecognitionAsync()
    {
        if (_isListening)
        {
            _logger.LogWarning("Already listening for speech");
            return;
        }

        try
        {
            var recognizer = GetOrCreateRecognizer();
            await recognizer.StartContinuousRecognitionAsync();
            _isListening = true;
            _logger.LogInformation("Started continuous speech recognition");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start continuous recognition");
            throw;
        }
    }

    public async Task StopContinuousRecognitionAsync()
    {
        if (!_isListening)
        {
            _logger.LogWarning("Not currently listening");
            return;
        }

        try
        {
            if (_recognizer != null)
            {
                await _recognizer.StopContinuousRecognitionAsync();
                _isListening = false;
                _logger.LogInformation("Stopped continuous speech recognition");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop continuous recognition");
            throw;
        }
    }

    public async Task<string?> RecognizeOnceAsync()
    {
        try
        {
            var recognizer = GetOrCreateRecognizer();
            var result = await recognizer.RecognizeOnceAsync();

            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                _logger.LogInformation("Recognized: {Text}", result.Text);
                return result.Text;
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                _logger.LogDebug("No speech could be recognized");
                return null;
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = CancellationDetails.FromResult(result);
                _logger.LogWarning("Recognition canceled: {Reason}", cancellation.Reason);

                if (cancellation.Reason == CancellationReason.Error)
                {
                    _logger.LogError("Error: {ErrorCode} - {ErrorDetails}",
                        cancellation.ErrorCode, cancellation.ErrorDetails);
                }
                return null;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recognize speech");
            throw;
        }
    }

    public void Dispose()
    {
        _recognizer?.Dispose();
        _recognizer = null;
    }
}
