# Komputa Chat Bot - Setup Guide

Welcome to Komputa! This guide will help you set up and start conversing with your AI assistant.

## What is Komputa?

Komputa is a memory-aware AI assistant powered by OpenAI's GPT-4. It remembers your conversations and provides contextual, personalized responses. You can interact with it through:
- **Web Interface**: Beautiful browser-based chat (recommended for beginners)
- **Console**: Command-line text interface
- **REST API**: Integrate with your own applications

## Prerequisites

1. **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **OpenAI API Key** - [Get one here](https://platform.openai.com/api-keys)

## Quick Start

### Step 1: Get Your OpenAI API Key

1. Go to https://platform.openai.com/api-keys
2. Create a new API key
3. Copy it (you'll need it in the next step)

### Step 2: Configure Your API Key

The API key is stored securely using .NET user secrets. Run this command in the project directory:

```bash
# For Web Interface (recommended)
cd src/Komputa.Presentation.WebAPI
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"

# For Console Interface
cd src/Komputa.Presentation.Console
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
```

Replace `your-api-key-here` with your actual OpenAI API key.

### Step 3: Choose Your Interface

#### Option A: Web Interface (Recommended) 🌐

1. **Start the web server:**
   ```bash
   cd src/Komputa.Presentation.WebAPI
   dotnet run
   ```

2. **Open your browser:**
   - Navigate to: http://localhost:5000
   - You'll see a beautiful chat interface
   - Start chatting with Komputa!

3. **Features:**
   - Modern, responsive design
   - Real-time conversation
   - Memory status button
   - Works on mobile and desktop

#### Option B: Console Interface 💻

1. **Run the console app:**
   ```bash
   cd src/Komputa.Presentation.Console
   dotnet run
   ```

2. **Start chatting:**
   - Type your message and press Enter
   - Type `memory` to check conversation memory
   - Type `exit` to quit

#### Option C: REST API 🔌

If you're a developer and want to integrate Komputa into your own application:

1. **Start the API server:**
   ```bash
   cd src/Komputa.Presentation.WebAPI
   dotnet run
   ```

2. **API Endpoints:**

   **Send a message:**
   ```bash
   POST http://localhost:5000/api/chat/message
   Content-Type: application/json

   {
     "message": "Hello Komputa!"
   }
   ```

   **Check memory status:**
   ```bash
   GET http://localhost:5000/api/chat/memory
   ```

   **Health check:**
   ```bash
   GET http://localhost:5000/api/chat/health
   ```

3. **Swagger Documentation:**
   - Navigate to: http://localhost:5000/swagger
   - Interactive API documentation and testing

## Using Startup Scripts

For easier startup, use the provided scripts:

### Windows:
```bash
# Web Interface
run-web.bat

# Console Interface
run-console.bat
```

### macOS/Linux:
```bash
# Web Interface
chmod +x run-web.sh
./run-web.sh

# Console Interface
chmod +x run-console.sh
./run-console.sh
```

## Memory System

Komputa has a sophisticated memory system that:
- Remembers important parts of your conversations
- Scores content based on importance
- Applies time-based decay to old memories
- Provides context-aware responses

Your conversation memories are stored in: `memory-bank/conversations/`

## Troubleshooting

### "AI provider not available"
- Check that you've set your OpenAI API key correctly
- Verify your API key is valid at https://platform.openai.com/api-keys
- Make sure you have credits in your OpenAI account

### "Cannot connect to API" (Web Interface)
- Ensure the API server is running (`dotnet run` in WebAPI project)
- Check that nothing else is using port 5000
- Try http://localhost:5000/api/chat/health to verify the API is running

### Port 5000 is already in use
- Edit `src/Komputa.Presentation.WebAPI/Properties/launchSettings.json`
- Change the port to something else (e.g., 5001)
- Update the API URL in `wwwroot/app.js` to match

### Build errors
- Make sure you have .NET 8.0 SDK installed: `dotnet --version`
- Restore packages: `dotnet restore`
- Clean and rebuild: `dotnet clean && dotnet build`

## Configuration

### Change AI Model
Edit `appsettings.json` in either Console or WebAPI project:

```json
{
  "OpenAI": {
    "Model": "gpt-4o",          // or "gpt-4-turbo", "gpt-3.5-turbo"
    "MaxTokens": 1000,
    "EnableFunctionCalling": true
  }
}
```

### Logging
Logs are written to:
- Console output
- Seq server (if running on http://localhost:5341)

To view structured logs, install Seq:
```bash
docker run -d --restart unless-stopped --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
```

## Next Steps

1. **Start chatting** - Try asking Komputa about various topics
2. **Test memory** - Have a conversation, then reference earlier topics
3. **Explore the code** - Check out the clean architecture in the `src/` directory
4. **Customize** - Modify prompts, adjust memory settings, or add new features

## Support

For issues, questions, or contributions, visit the project repository.

## Security Note

Never commit your API key to version control! The user secrets system keeps your key secure and separate from your code.

---

Happy chatting with Komputa! 🧠✨
