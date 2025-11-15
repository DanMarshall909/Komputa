using Komputa.Application.Interfaces;
using Komputa.Infrastructure.Services;
using Komputa.Presentation.Voice;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

class Program
{
    static async Task Main()
    {
        // Load configuration including user secrets
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>();

        var config = configBuilder.Build();

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .CreateLogger();

        var logger = Log.ForContext<Program>();

        try
        {
            logger.Information("🧠 Starting Komputa Voice Assistant");

            // Set up dependency injection
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddLogging(builder => builder.ClearProviders().AddSerilog())
                .AddHttpClient()
                // Memory and conversation services
                .AddSingleton<IMemoryStore, JsonMemoryStore>()
                .AddSingleton<IContentScorer, VoiceAssistantContentScorer>()
                .AddSingleton<IWebSearchService, WebSearchService>()
                .AddSingleton<ILanguageModelProvider, OpenAIProvider>()
                .AddSingleton<MemoryAwareConversationService>()
                // Voice services
                .AddSingleton<ISpeechRecognitionService, AzureSpeechRecognitionService>()
                .AddSingleton<ISpeechSynthesisService, AzureSpeechSynthesisService>()
                .AddSingleton<IWakeWordDetector, KeywordSpottingWakeWordDetector>()
                // Voice orchestrator
                .AddSingleton<VoiceAssistantOrchestrator>()
                .BuildServiceProvider();

            // Verify configurations
            var openAIProvider = services.GetRequiredService<ILanguageModelProvider>();
            logger.Information("AI Provider: {ProviderName} ({Status})",
                openAIProvider.ProviderName,
                openAIProvider.IsAvailable ? "Available" : "Not Available");

            if (!openAIProvider.IsAvailable)
            {
                logger.Warning("⚠️  OpenAI provider not available - check configuration");
                Console.WriteLine("⚠️  Warning: OpenAI provider not available. Please check your OpenAI API key.");
                Console.WriteLine();
            }

            // Check Azure Speech configuration
            var speechKey = config["AzureSpeech:SubscriptionKey"];
            var speechRegion = config["AzureSpeech:Region"];

            if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechRegion))
            {
                logger.Error("Azure Speech configuration missing");
                Console.WriteLine("❌ Error: Azure Speech Service not configured.");
                Console.WriteLine();
                Console.WriteLine("Please set up your Azure Speech Service credentials:");
                Console.WriteLine("1. Create an Azure Speech Service resource at https://portal.azure.com");
                Console.WriteLine("2. Run setup-voice.bat (Windows) or ./setup-voice.sh (Unix)");
                Console.WriteLine("   OR manually run:");
                Console.WriteLine("   dotnet user-secrets set \"AzureSpeech:SubscriptionKey\" \"your-key\"");
                Console.WriteLine("   dotnet user-secrets set \"AzureSpeech:Region\" \"your-region\"");
                Console.WriteLine();
                return;
            }

            logger.Information("Azure Speech: Region {Region}", speechRegion);

            // Start the voice assistant
            var orchestrator = services.GetRequiredService<VoiceAssistantOrchestrator>();

            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                logger.Information("Shutdown requested");
                await orchestrator.StopAsync();
                Environment.Exit(0);
            };

            await orchestrator.StartAsync();

            // Keep running until Ctrl+C
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Fatal error during voice assistant startup");
            Console.WriteLine($"❌ Fatal Error: {ex.Message}");
            Console.WriteLine("Check the logs for more details.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
