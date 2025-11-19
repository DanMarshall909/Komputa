using Komputa.Application.Interfaces;
using Komputa.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS for web clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register application services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IMemoryStore, JsonMemoryStore>();
builder.Services.AddSingleton<IContentScorer, VoiceAssistantContentScorer>();
builder.Services.AddSingleton<IWebSearchService, WebSearchService>();
builder.Services.AddSingleton<ILanguageModelProvider, OpenAIProvider>();
builder.Services.AddSingleton<MemoryAwareConversationService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Serve static files for the chat interface
app.UseStaticFiles();

Log.Information("🧠 Komputa Web API starting on {Url}", builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000");

app.Run();
