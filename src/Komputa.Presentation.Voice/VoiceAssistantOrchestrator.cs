using Komputa.Application.Interfaces;
using Komputa.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Komputa.Presentation.Voice;

/// <summary>
/// Orchestrates the voice assistant interaction flow:
/// 1. Listen for wake word ("hey komputa")
/// 2. Activate and listen for user input
/// 3. Send to AI for processing
/// 4. Speak the response
/// 5. Return to listening for wake word
/// </summary>
public class VoiceAssistantOrchestrator : IDisposable
{
    private readonly ISpeechRecognitionService _speechRecognition;
    private readonly ISpeechSynthesisService _speechSynthesis;
    private readonly MemoryAwareConversationService _conversationService;
    private readonly ILogger<VoiceAssistantOrchestrator> _logger;

    private bool _isProcessingRequest;
    private bool _isRunning;
    private readonly string _wakeWord = "hey komputa";

    public VoiceAssistantOrchestrator(
        ISpeechRecognitionService speechRecognition,
        ISpeechSynthesisService speechSynthesis,
        MemoryAwareConversationService conversationService,
        ILogger<VoiceAssistantOrchestrator> logger)
    {
        _speechRecognition = speechRecognition;
        _speechSynthesis = speechSynthesis;
        _conversationService = conversationService;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        if (_isRunning)
        {
            _logger.LogWarning("Voice assistant is already running");
            return;
        }

        _logger.LogInformation("🧠 Starting Komputa Voice Assistant...");
        Console.WriteLine("🧠 Komputa Voice Assistant");
        Console.WriteLine("======================================");
        Console.WriteLine($"💬 Say '{_wakeWord}' to activate");
        Console.WriteLine("🔴 Press Ctrl+C to exit");
        Console.WriteLine();

        // Wire up speech recognition events
        _speechRecognition.SpeechRecognized += OnSpeechRecognized;
        _speechRecognition.SessionStarted += OnSessionStarted;
        _speechRecognition.SessionStopped += OnSessionStopped;

        // Start continuous listening for wake word
        await _speechRecognition.StartContinuousRecognitionAsync();
        _isRunning = true;

        _logger.LogInformation("Voice assistant started. Listening for wake word: '{WakeWord}'", _wakeWord);
        Console.WriteLine($"👂 Listening for '{_wakeWord}'...");
    }

    public async Task StopAsync()
    {
        if (!_isRunning)
        {
            return;
        }

        _logger.LogInformation("Stopping voice assistant...");

        await _speechRecognition.StopContinuousRecognitionAsync();

        _speechRecognition.SpeechRecognized -= OnSpeechRecognized;
        _speechRecognition.SessionStarted -= OnSessionStarted;
        _speechRecognition.SessionStopped -= OnSessionStopped;

        _isRunning = false;
        _logger.LogInformation("Voice assistant stopped");
        Console.WriteLine("👋 Goodbye!");
    }

    private async void OnSpeechRecognized(object? sender, string recognizedText)
    {
        try
        {
            _logger.LogInformation("Recognized: {Text}", recognizedText);

            // Check for wake word
            if (!_isProcessingRequest && ContainsWakeWord(recognizedText))
            {
                await HandleWakeWordDetectedAsync();
                return;
            }

            // If we're processing a request, this is the user's command/question
            if (_isProcessingRequest)
            {
                await HandleUserInputAsync(recognizedText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling recognized speech");
        }
    }

    private bool ContainsWakeWord(string text)
    {
        // Case-insensitive check for wake word
        return text.ToLowerInvariant().Contains(_wakeWord.ToLowerInvariant()) ||
               text.ToLowerInvariant().Contains("hey computer") ||  // Common misrecognition
               text.ToLowerInvariant().Contains("hey compute");      // Common misrecognition
    }

    private async Task HandleWakeWordDetectedAsync()
    {
        _logger.LogInformation("Wake word detected!");
        Console.WriteLine($"\n✅ Wake word detected!");

        // Play activation sound (simple beep)
        Console.Beep(800, 100);
        await Task.Delay(50);
        Console.Beep(1000, 100);

        // Speak acknowledgment
        await _speechSynthesis.SpeakAsync("Yes?");

        Console.WriteLine("🎤 Listening... (speak your question)");
        _isProcessingRequest = true;
    }

    private async Task HandleUserInputAsync(string userInput)
    {
        try
        {
            Console.WriteLine($"You: {userInput}");
            _logger.LogInformation("Processing user input: {Input}", userInput);

            // Show thinking indicator
            Console.WriteLine("🤔 Thinking...");

            // Get AI response
            var response = await _conversationService.GetResponseWithMemoryAsync(userInput);

            _logger.LogInformation("Generated response: {Response}", response);
            Console.WriteLine($"Komputa: {response}");
            Console.WriteLine();

            // Speak the response
            await _speechSynthesis.SpeakAsync(response);

            // Wait for speech to complete before returning to wake word listening
            await Task.Delay(500); // Small delay to ensure clean state transition

            Console.WriteLine($"👂 Listening for '{_wakeWord}'...");
            _isProcessingRequest = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user input");

            var errorMessage = "Sorry, I encountered an error processing your request.";
            Console.WriteLine($"❌ {errorMessage}");
            await _speechSynthesis.SpeakAsync(errorMessage);

            Console.WriteLine($"👂 Listening for '{_wakeWord}'...");
            _isProcessingRequest = false;
        }
    }

    private void OnSessionStarted(object? sender, EventArgs e)
    {
        _logger.LogDebug("Speech recognition session started");
    }

    private void OnSessionStopped(object? sender, EventArgs e)
    {
        _logger.LogDebug("Speech recognition session stopped");

        if (_isRunning)
        {
            // Session stopped unexpectedly, try to restart
            _logger.LogWarning("Speech recognition session stopped unexpectedly, attempting to restart...");
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                if (_isRunning)
                {
                    await _speechRecognition.StartContinuousRecognitionAsync();
                }
            });
        }
    }

    public void Dispose()
    {
        if (_isRunning)
        {
            StopAsync().Wait();
        }
    }
}
