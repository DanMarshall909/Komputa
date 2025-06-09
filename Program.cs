namespace Komputa;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Komputa.Interfaces;
using Komputa.Services;

class Program
{
	static async Task Main()
	{
		Console.WriteLine("🧠 Komputa - Memory-Aware AI Assistant");
		Console.WriteLine("======================================");

        // Load configuration including user secrets
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>(); // Load user secrets for secure API key storage

        var config = configBuilder.Build();

		// Set up dependency injection with new memory-aware services
		var services = new ServiceCollection()
			.AddSingleton<IConfiguration>(config)
			.AddHttpClient()
			.AddSingleton<IMemoryStore, JsonMemoryStore>()
			.AddSingleton<IContentScorer, VoiceAssistantContentScorer>()
			.AddSingleton<ILanguageModelProvider, OpenAIProvider>()
			.AddSingleton<MemoryAwareConversationService>()
			.BuildServiceProvider();

		var conversationService = services.GetRequiredService<MemoryAwareConversationService>();
		var aiProvider = services.GetRequiredService<ILanguageModelProvider>();

		Console.WriteLine($"🤖 AI Provider: {aiProvider.ProviderName} ({(aiProvider.IsAvailable ? "Available" : "Not Available")})");
		Console.WriteLine("💭 Memory system initialized");
		Console.WriteLine();

		if (!aiProvider.IsAvailable)
		{
			Console.WriteLine("⚠️  Warning: AI provider not available. Please check your configuration.");
			Console.WriteLine();
		}

		Console.WriteLine("Commands:");
		Console.WriteLine("- Type 'memory' to check conversation memory");
		Console.WriteLine("- Type 'exit' to quit");
		Console.WriteLine();

		while (true)
		{
			Console.Write("You: ");
			string? input = Console.ReadLine();

			if (string.IsNullOrWhiteSpace(input))
				continue;

			if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine("👋 Goodbye!");
				break;
			}

			if (input.Equals("memory", StringComparison.OrdinalIgnoreCase))
			{
				var memoryStatus = await conversationService.GetMemoryStatusAsync();
				Console.WriteLine($"💭 Memory Status: {memoryStatus}");
				continue;
			}

			try
			{
				string response = await conversationService.GetResponseWithMemoryAsync(input);
				Console.WriteLine($"Komputa: {response}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error: {ex.Message}");
			}
		}
	}
}
