# 🧠 Komputa - Memory-Aware AI Assistant

Komputa is an intelligent conversational AI bot powered by OpenAI's GPT-4, featuring a sophisticated memory system that remembers your conversations and provides contextual, personalized responses.

## ✨ Features

- 🧠 **Memory-Aware**: Remembers and learns from your conversations
- 💬 **Multiple Interfaces**: Web UI, Console, and REST API
- 🎨 **Beautiful Web Interface**: Modern, responsive chat design
- 🏗️ **Clean Architecture**: Built with DDD principles and SOLID design
- 🔒 **Secure**: API keys stored safely using .NET user secrets
- 📊 **Observable**: Structured logging with Serilog

## 🚀 Quick Start

### 1. Install Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [OpenAI API Key](https://platform.openai.com/api-keys)

### 2. Setup API Key

**Windows:**
```bash
setup-api-key.bat
```

**macOS/Linux:**
```bash
chmod +x setup-api-key.sh
./setup-api-key.sh
```

### 3. Start Chatting

**Web Interface (Recommended):**
```bash
# Windows
run-web.bat

# macOS/Linux
chmod +x run-web.sh
./run-web.sh
```

Then open http://localhost:5000 in your browser.

**Console Interface:**
```bash
# Windows
run-console.bat

# macOS/Linux
chmod +x run-console.sh
./run-console.sh
```

## 📖 Full Documentation

For complete setup instructions, troubleshooting, and advanced configuration, see [CHAT_BOT_SETUP.md](CHAT_BOT_SETUP.md).

## 🏗️ Architecture

Komputa follows Clean Architecture with Domain-Driven Design:

```
src/
├── Komputa.Domain/              # Core business logic
│   ├── Entities/                # Conversation, MemoryItem
│   ├── ValueObjects/            # ContentType, MemoryScore
│   └── Models/                  # Data transfer objects
├── Komputa.Application/         # Application services
│   └── Interfaces/              # Service contracts
├── Komputa.Infrastructure/      # External integrations
│   └── Services/                # OpenAI, Memory, Search
└── Komputa.Presentation/        # User interfaces
    ├── Console/                 # CLI interface
    └── WebAPI/                  # REST API + Web UI
```

## 🔌 API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/chat/message` | POST | Send a message and get a response |
| `/api/chat/memory` | GET | Check conversation memory status |
| `/api/chat/health` | GET | Health check |

Visit http://localhost:5000/swagger for interactive API documentation.

## 💡 Usage Examples

### Web Interface
1. Run `run-web.bat` (Windows) or `./run-web.sh` (macOS/Linux)
2. Open http://localhost:5000
3. Start chatting!

### Console
```bash
cd src/Komputa.Presentation.Console
dotnet run
```

### API
```bash
curl -X POST http://localhost:5000/api/chat/message \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello Komputa!"}'
```

## 🧪 Testing

Run all tests:
```bash
dotnet test
```

Run specific test projects:
```bash
dotnet test Tests/Komputa.Tests.Domain
dotnet test Tests/Komputa.Tests.Application
dotnet test Tests/Komputa.Tests.Integration
```

## 🛠️ Configuration

Edit `appsettings.json` to configure:
- AI model (gpt-4o, gpt-4-turbo, gpt-3.5-turbo)
- Max tokens
- Logging levels
- Seq server URL

## 📝 Memory System

Komputa's memory system:
- Scores content importance automatically
- Applies time-based decay
- Retrieves relevant context for responses
- Stores conversations in `memory-bank/conversations/`

## 🤝 Contributing

Contributions are welcome! The codebase follows:
- Clean Architecture principles
- SOLID design patterns
- Comprehensive testing (xUnit, SpecFlow)
- BDD scenarios with Gherkin

## 📄 License

See LICENSE file for details.

## 🙏 Acknowledgments

- Built with .NET 8.0
- Powered by OpenAI GPT-4
- Structured logging with Serilog
- Testing with xUnit and SpecFlow

---

**Start conversing with Komputa today!** 🚀

For detailed setup instructions, see [CHAT_BOT_SETUP.md](CHAT_BOT_SETUP.md)
