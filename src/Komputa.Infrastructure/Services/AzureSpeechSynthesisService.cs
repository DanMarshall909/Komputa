using Komputa.Application.Interfaces;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Komputa.Infrastructure.Services;

public class AzureSpeechSynthesisService : ISpeechSynthesisService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureSpeechSynthesisService> _logger;
    private SpeechSynthesizer? _synthesizer;
    private bool _isSpeaking;

    public event EventHandler? SpeechStarted;
    public event EventHandler? SpeechCompleted;

    public bool IsSpeaking => _isSpeaking;

    public AzureSpeechSynthesisService(
        IConfiguration configuration,
        ILogger<AzureSpeechSynthesisService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private SpeechSynthesizer GetOrCreateSynthesizer()
    {
        if (_synthesizer != null)
            return _synthesizer;

        var subscriptionKey = _configuration["AzureSpeech:SubscriptionKey"];
        var region = _configuration["AzureSpeech:Region"];

        if (string.IsNullOrEmpty(subscriptionKey) || string.IsNullOrEmpty(region))
        {
            throw new InvalidOperationException(
                "Azure Speech configuration is missing. Please set AzureSpeech:SubscriptionKey and AzureSpeech:Region in user secrets or appsettings.json");
        }

        var config = SpeechConfig.FromSubscription(subscriptionKey, region);

        // Set voice name - default to a natural sounding voice
        var voiceName = _configuration["AzureSpeech:VoiceName"] ?? "en-US-AriaNeural";
        config.SpeechSynthesisVoiceName = voiceName;

        var audioConfig = AudioConfig.FromDefaultSpeakerOutput();
        _synthesizer = new SpeechSynthesizer(config, audioConfig);

        // Wire up events
        _synthesizer.SynthesisStarted += (s, e) =>
        {
            _logger.LogInformation("Speech synthesis started");
            _isSpeaking = true;
            SpeechStarted?.Invoke(this, EventArgs.Empty);
        };

        _synthesizer.SynthesisCompleted += (s, e) =>
        {
            _logger.LogInformation("Speech synthesis completed");
            _isSpeaking = false;
            SpeechCompleted?.Invoke(this, EventArgs.Empty);
        };

        _synthesizer.SynthesisCanceled += (s, e) =>
        {
            _logger.LogWarning("Speech synthesis canceled: {Reason}", e.Reason);
            if (e.Reason == CancellationReason.Error)
            {
                _logger.LogError("Error: {ErrorCode} - {ErrorDetails}", e.ErrorCode, e.ErrorDetails);
            }
            _isSpeaking = false;
        };

        return _synthesizer;
    }

    public async Task SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Attempted to speak empty text");
            return;
        }

        try
        {
            _logger.LogInformation("Speaking: {Text}", text);
            var synthesizer = GetOrCreateSynthesizer();
            var result = await synthesizer.SpeakTextAsync(text);

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                _logger.LogError("Speech synthesis canceled: {Reason} - {ErrorDetails}",
                    cancellation.Reason, cancellation.ErrorDetails);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synthesize speech");
            throw;
        }
    }

    public async Task SpeakAsync(string text, string voiceName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Attempted to speak empty text");
            return;
        }

        try
        {
            _logger.LogInformation("Speaking with voice {Voice}: {Text}", voiceName, text);
            var synthesizer = GetOrCreateSynthesizer();

            // Use SSML to specify voice
            var ssml = $@"
                <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
                    <voice name='{voiceName}'>
                        {System.Security.SecurityElement.Escape(text)}
                    </voice>
                </speak>";

            var result = await synthesizer.SpeakSsmlAsync(ssml);

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                _logger.LogError("Speech synthesis canceled: {Reason} - {ErrorDetails}",
                    cancellation.Reason, cancellation.ErrorDetails);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synthesize speech with voice {Voice}", voiceName);
            throw;
        }
    }

    public Task StopSpeakingAsync()
    {
        // Note: Azure Speech SDK doesn't provide a direct stop method for ongoing synthesis
        // We would need to dispose and recreate the synthesizer
        if (_synthesizer != null)
        {
            _synthesizer.Dispose();
            _synthesizer = null;
            _isSpeaking = false;
            _logger.LogInformation("Stopped speech synthesis");
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _synthesizer?.Dispose();
        _synthesizer = null;
    }
}
