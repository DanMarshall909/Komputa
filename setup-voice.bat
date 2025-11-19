@echo off
echo.
echo ===============================================
echo   Komputa Voice Assistant Setup
echo ===============================================
echo.
echo This script will configure Azure Speech Service
echo and OpenAI for voice interaction.
echo.
echo.

echo Step 1: OpenAI API Key
echo ----------------------
set /p OPENAI_KEY="Enter your OpenAI API Key: "

if "%OPENAI_KEY%"=="" (
    echo.
    echo Error: No API key provided!
    echo.
    pause
    exit /b 1
)

echo.
echo Step 2: Azure Speech Service
echo -----------------------------
echo.
echo Get your Azure Speech credentials from:
echo   https://portal.azure.com
echo.
echo 1. Create a Speech Service resource
echo 2. Get your key and region from the Keys page
echo.

set /p SPEECH_KEY="Enter your Azure Speech Subscription Key: "
set /p SPEECH_REGION="Enter your Azure Speech Region (e.g., eastus, westus): "

if "%SPEECH_KEY%"=="" (
    echo.
    echo Error: No Speech key provided!
    echo.
    pause
    exit /b 1
)

if "%SPEECH_REGION%"=="" (
    echo.
    echo Error: No region provided!
    echo.
    pause
    exit /b 1
)

echo.
echo Configuring Voice Assistant...
cd src\Komputa.Presentation.Voice

echo Setting OpenAI API key...
dotnet user-secrets set "OpenAI:ApiKey" "%OPENAI_KEY%"

echo Setting Azure Speech subscription key...
dotnet user-secrets set "AzureSpeech:SubscriptionKey" "%SPEECH_KEY%"

echo Setting Azure Speech region...
dotnet user-secrets set "AzureSpeech:Region" "%SPEECH_REGION%"

cd ..\..

echo.
echo ===============================================
echo   Setup Complete!
echo ===============================================
echo.
echo Your voice assistant is ready to use!
echo.
echo To start the voice assistant, run:
echo   run-voice.bat
echo.
echo Then say "hey komputa" to activate it.
echo.
pause
