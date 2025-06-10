using Komputa.Application.Interfaces;
using Komputa.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Komputa.Presentation.Console;

class Program
{
	static async Task Main()
	{
        // Load configuration including user secrets
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>(); // Load user secrets for secure API key storage

        var config = configBuilder.Build();

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .CreateLogger();

        var logger = Log.ForContext<Program>();

        try
        {
            logger.Information("🧠 Starting Komputa - Memory-Aware AI Assistant");
            
            System.Console.WriteLine("🧠 Komputa - Memory-Aware AI Assistant");
            System.Console.WriteLine("======================================");

            // Set up dependency injection with logging
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddLogging(builder => builder.ClearProviders().AddSerilog())
                .AddHttpClient()
                .AddSingleton<IMemoryStore, JsonMemoryStore>()
                .AddSingleton<IContentScorer, VoiceAssistantContentScorer>()
                .AddSingleton<IWebSearchService, WebSearchService>()
                .AddSingleton<ILanguageModelProvider, OpenAIProvider>()
                .AddSingleton<MemoryAwareConversationService>()
                .BuildServiceProvider();

            var conversationService = services.GetRequiredService<MemoryAwareConversationService>();
            var aiProvider = services.GetRequiredService<ILanguageModelProvider>();

            logger.Information("AI Provider: {ProviderName} ({Status})", 
                aiProvider.ProviderName, 
                aiProvider.IsAvailable ? "Available" : "Not Available");

            System.Console.WriteLine($"🤖 AI Provider: {aiProvider.ProviderName} ({(aiProvider.IsAvailable ? "Available" : "Not Available")})");
            System.Console.WriteLine("💭 Memory system initialized");
            System.Console.WriteLine();

            if (!aiProvider.IsAvailable)
            {
                logger.Warning("AI provider not available - check configuration");
                System.Console.WriteLine("⚠️  Warning: AI provider not available. Please check your configuration.");
                System.Console.WriteLine();
            }

            System.Console.WriteLine("Commands:");
            System.Console.WriteLine("- Type 'memory' to check conversation memory");
            System.Console.WriteLine("- Type 'exit' to quit");
            System.Console.WriteLine();

            while (true)
            {
                try
                {
                    System.Console.Write("You: ");
                    string? input = System.Console.ReadLine();

                    // Add explicit null check and logging
                    if (input == null)
                    {
                        logger.Warning("Console.ReadLine() returned null - possible input stream issue");
                        System.Console.WriteLine("⚠️  Input stream error. Please try again or type 'exit' to quit.");
                        await Task.Delay(1000); // Prevent rapid loop if there's a persistent issue
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(input))
                    {
                        logger.Debug("Empty input received, prompting again");
                        continue;
                    }

                    logger.Debug("User input received: {InputLength} characters", input.Length);

                    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        logger.Information("User exited application");
                        System.Console.WriteLine("👋 Goodbye!");
                        break;
                    }

                    if (input.Equals("memory", StringComparison.OrdinalIgnoreCase))
                    {
                        var memoryStatus = await conversationService.GetMemoryStatusAsync();
                        logger.Information("Memory status requested: {Status}", memoryStatus);
                        System.Console.WriteLine($"💭 Memory Status: {memoryStatus}");
                        continue;
                    }

                    logger.Information("User input: {Input}", input);
                    string response = await conversationService.GetResponseWithMemoryAsync(input);
                    logger.Information("AI response generated successfully");
                    System.Console.WriteLine($"Komputa: {response}");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error in main conversation loop");
                    System.Console.WriteLine($"❌ Loop Error: {ex.Message}");
                    
                    // Add a small delay to prevent rapid error loops
                    await Task.Delay(1000);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Fatal error during application startup");
            System.Console.WriteLine($"❌ Fatal Error: {ex.Message}");
        }
        finally
        {
            Log.CloseAndFlush();
        }
	}
}
