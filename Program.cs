namespace Komputa;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main()
    {
        // Load configuration
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        if (environment == "Development")
        {
            configBuilder.AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true);
        }

        var config = configBuilder.Build();

        // Set up dependency injection
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config)
            .AddHttpClient()
            .AddSingleton<AssistantService>()
            .BuildServiceProvider();

        var assistant = services.GetRequiredService<AssistantService>();

        Console.WriteLine("Welcome to Komputa - Your AI Assistant!");

        while (true)
        {
            Console.Write("You: ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Goodbye!");
                break;
            }

            string response = await assistant.GetResponseAsync(input);
            Console.WriteLine($"Komputa: {response}");
        }
    }
}

public class AssistantService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public AssistantService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"] ??
                  throw new InvalidOperationException("OpenAI API key is missing from configuration.");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<string> GetResponseAsync(string prompt)
    {
        var request = new
        {
            model = "gpt-4",
            messages = new[] { new { role = "user", content = prompt } }
        };

        var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request);

        if (!response.IsSuccessStatusCode)
        {
            return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
        }

        var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();

        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response";
    }
}

public class OpenAiResponse
{
    [JsonPropertyName("choices")] public List<Choice>? Choices { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")] public Message? Message { get; set; }
}

public class Message
{
    [JsonPropertyName("content")] public string? Content { get; set; }
}