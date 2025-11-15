# Komputa Voice Assistant - Setup Guide

Transform Komputa into your personal voice assistant, just like Google Nest or Amazon Alexa!

## What You'll Get

- 🎙️ **Wake word activation**: Say "hey komputa" to activate
- 🗣️ **Voice conversations**: Speak naturally with your AI assistant
- 🔊 **Voice responses**: Komputa speaks back to you
- 🧠 **Memory-aware**: Remembers your conversations across sessions
- 🎯 **Hands-free**: No typing required

## Prerequisites

### 1. .NET 8.0 SDK
Download from: https://dotnet.microsoft.com/download/dotnet/8.0

### 2. OpenAI API Key
Get one from: https://platform.openai.com/api-keys

### 3. Azure Speech Service (FREE tier available)

#### Why Azure Speech?
- Industry-leading speech recognition accuracy
- Natural-sounding text-to-speech voices
- FREE tier includes:
  - 5 hours/month of speech-to-text
  - 0.5 million characters/month of text-to-speech
  - More than enough for personal use!

#### Get Your Azure Speech Credentials:

1. **Create Azure Account** (if you don't have one):
   - Go to https://azure.microsoft.com/free
   - Sign up for free account (includes $200 credit for 30 days)

2. **Create Speech Service Resource**:
   - Go to https://portal.azure.com
   - Click "Create a resource"
   - Search for "Speech"
   - Click "Speech" by Microsoft
   - Click "Create"
   - Fill in:
     - **Subscription**: Your subscription
     - **Resource Group**: Create new or use existing
     - **Region**: Choose closest to you (e.g., "East US", "West US", "West Europe")
     - **Name**: Any name you like (e.g., "komputa-speech")
     - **Pricing tier**: Select "Free F0" (FREE!)
   - Click "Review + create"
   - Click "Create"

3. **Get Your Credentials**:
   - Once created, go to your Speech resource
   - Click "Keys and Endpoint" in the left menu
   - Copy **Key 1** (your subscription key)
   - Note your **Location/Region** (e.g., "eastus")

## Quick Setup

### Automatic Setup (Recommended)

**Windows:**
```bash
setup-voice.bat
```

**macOS/Linux:**
```bash
chmod +x setup-voice.sh
./setup-voice.sh
```

The script will ask for:
1. OpenAI API key
2. Azure Speech subscription key
3. Azure Speech region

### Manual Setup

If you prefer to configure manually:

```bash
cd src/Komputa.Presentation.Voice

# Set OpenAI API key
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-key"

# Set Azure Speech credentials
dotnet user-secrets set "AzureSpeech:SubscriptionKey" "your-azure-speech-key"
dotnet user-secrets set "AzureSpeech:Region" "your-region"
```

Replace:
- `your-openai-key` with your OpenAI API key
- `your-azure-speech-key` with Azure Speech Key 1
- `your-region` with your Azure region (e.g., `eastus`, `westus`, `westeurope`)

## Starting the Voice Assistant

### Using Startup Scripts

**Windows:**
```bash
run-voice.bat
```

**macOS/Linux:**
```bash
chmod +x run-voice.sh
./run-voice.sh
```

### Manual Start

```bash
cd src/Komputa.Presentation.Voice
dotnet run
```

## How to Use

1. **Start the assistant** using one of the methods above

2. **Wait for the "Listening" message**:
   ```
   🧠 Komputa Voice Assistant
   ======================================
   💬 Say 'hey komputa' to activate
   🔴 Press Ctrl+C to exit

   👂 Listening for 'hey komputa'...
   ```

3. **Activate with wake word**:
   - Say **"hey komputa"**
   - You'll hear two beeps and "Yes?"
   - The assistant is now listening for your question

4. **Ask your question**:
   - Speak naturally: "What's the weather like today?"
   - Komputa will think and respond both on screen and vocally

5. **Continue the conversation**:
   - Say "hey komputa" again to ask another question
   - Komputa remembers previous conversation context

6. **Stop the assistant**:
   - Press **Ctrl+C** to exit

## Example Interaction

```
You: "hey komputa"
🎵 [beep beep]
Komputa: "Yes?"

You: "Tell me a joke about programming"
🤔 Thinking...
Komputa: "Why do programmers prefer dark mode? Because light attracts bugs!"

👂 Listening for 'hey komputa'...

You: "hey komputa"
🎵 [beep beep]
Komputa: "Yes?"

You: "Tell me another one"
🤔 Thinking...
Komputa: "Here's another one! Why did the programmer quit his job?
         Because he didn't get arrays!"
```

## Configuration Options

Edit `src/Komputa.Presentation.Voice/appsettings.json` to customize:

### Voice Settings

```json
{
  "AzureSpeech": {
    "Language": "en-US",
    "VoiceName": "en-US-AriaNeural"
  }
}
```

**Popular Voice Options:**
- `en-US-AriaNeural` - Friendly female voice (default)
- `en-US-GuyNeural` - Professional male voice
- `en-US-JennyNeural` - Conversational female voice
- `en-US-DavisNeural` - Warm male voice
- `en-GB-SoniaNeural` - British female voice
- `en-GB-RyanNeural` - British male voice

Full list: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support?tabs=tts

### Wake Word

```json
{
  "WakeWord": "hey komputa"
}
```

You can change this, but the speech recognition works best with 2-3 word phrases.

### AI Model

```json
{
  "OpenAI": {
    "Model": "gpt-4o",
    "MaxTokens": 1000
  }
}
```

Options: `gpt-4o`, `gpt-4-turbo`, `gpt-3.5-turbo`

## Troubleshooting

### "Azure Speech Service not configured"
- Make sure you ran the setup script
- Verify your credentials are correct
- Check you selected the right region

### Speech not recognized
- Check your microphone is working
- Speak clearly and at normal volume
- Ensure your microphone is set as default input device
- Try moving closer to the microphone

### No audio output
- Check your speakers/headphones are connected
- Verify default audio output device is correct
- Check volume is not muted

### "Error: No speech could be recognized"
- Reduce background noise
- Speak more slowly and clearly
- Check your internet connection (Azure Speech requires internet)

### Wake word not detected
- Try saying "hey computer" or "hey compute" (common variations)
- Speak the wake word clearly with a slight pause after "hey"
- Make sure continuous recognition is running (check logs)

### High latency / Slow responses
- Check your internet connection speed
- Choose an Azure region closer to you
- Consider using a faster OpenAI model (gpt-3.5-turbo)

## Advanced: Custom Wake Word Model

For better wake word accuracy, you can create a custom keyword model:

1. Go to https://speech.microsoft.com/customkeyword
2. Sign in with your Azure account
3. Create a new custom keyword project
4. Follow the wizard to generate a model file
5. Download the `.table` file
6. Add to configuration:

```bash
dotnet user-secrets set "AzureSpeech:KeywordModelPath" "/path/to/your-keyword.table"
```

## Cost Considerations

### Azure Speech (FREE Tier)
- **Speech-to-Text**: 5 hours/month
- **Text-to-Speech**: 0.5 million characters/month
- **Typical usage**: ~2-3 hours of conversation = ~150,000 characters
- **Enough for**: Daily use for personal assistant

### OpenAI
- Charges per token (words)
- GPT-4o: ~$0.02 per conversation (input + output)
- GPT-3.5-turbo: ~$0.002 per conversation
- **Estimate**: $1-5/month for regular use

## Privacy & Security

- All credentials stored in .NET user secrets (NOT in code)
- Voice data sent to Azure Speech Service (Microsoft)
- Text processed by OpenAI (as with any ChatGPT usage)
- Conversation memories stored locally in `memory-bank/conversations/`
- No data shared with third parties beyond Azure/OpenAI

## What's Next?

Once you have the voice assistant running:

1. **Test it thoroughly** - Try different questions and commands
2. **Customize the voice** - Pick your favorite Azure Neural Voice
3. **Adjust wake word sensitivity** - Tune for your environment
4. **Set up auto-start** - Make it run on system startup (advanced)

## Comparison with Other Voice Assistants

| Feature | Komputa | Google Nest | Alexa |
|---------|---------|-------------|-------|
| **Memory** | ✅ Remembers conversations | ❌ Limited | ❌ Limited |
| **AI Model** | ✅ GPT-4 | Google AI | Amazon AI |
| **Customizable** | ✅ Fully open source | ❌ Closed | ❌ Closed |
| **Privacy** | ✅ You control data | ❌ Google servers | ❌ Amazon servers |
| **Offline** | ❌ Requires internet | ✅ Some features | ✅ Some features |
| **Cost** | ~$5/month | $50-100 device | $30-100 device |
| **Smart Home** | ❌ Not yet | ✅ Full integration | ✅ Full integration |

---

**Ready to start?** Run `setup-voice.bat` (Windows) or `./setup-voice.sh` (Unix) and say "hey komputa"!

For issues or questions, check the main README.md or project documentation.
