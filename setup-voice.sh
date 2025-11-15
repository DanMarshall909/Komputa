#!/bin/bash

echo ""
echo "==============================================="
echo "  Komputa Voice Assistant Setup"
echo "==============================================="
echo ""
echo "This script will configure Azure Speech Service"
echo "and OpenAI for voice interaction."
echo ""
echo ""

echo "Step 1: OpenAI API Key"
echo "----------------------"
read -p "Enter your OpenAI API Key: " OPENAI_KEY

if [ -z "$OPENAI_KEY" ]; then
    echo ""
    echo "Error: No API key provided!"
    echo ""
    exit 1
fi

echo ""
echo "Step 2: Azure Speech Service"
echo "-----------------------------"
echo ""
echo "Get your Azure Speech credentials from:"
echo "  https://portal.azure.com"
echo ""
echo "1. Create a Speech Service resource"
echo "2. Get your key and region from the Keys page"
echo ""

read -p "Enter your Azure Speech Subscription Key: " SPEECH_KEY
read -p "Enter your Azure Speech Region (e.g., eastus, westus): " SPEECH_REGION

if [ -z "$SPEECH_KEY" ]; then
    echo ""
    echo "Error: No Speech key provided!"
    echo ""
    exit 1
fi

if [ -z "$SPEECH_REGION" ]; then
    echo ""
    echo "Error: No region provided!"
    echo ""
    exit 1
fi

echo ""
echo "Configuring Voice Assistant..."
cd src/Komputa.Presentation.Voice

echo "Setting OpenAI API key..."
dotnet user-secrets set "OpenAI:ApiKey" "$OPENAI_KEY"

echo "Setting Azure Speech subscription key..."
dotnet user-secrets set "AzureSpeech:SubscriptionKey" "$SPEECH_KEY"

echo "Setting Azure Speech region..."
dotnet user-secrets set "AzureSpeech:Region" "$SPEECH_REGION"

cd ../..

echo ""
echo "==============================================="
echo "  Setup Complete!"
echo "==============================================="
echo ""
echo "Your voice assistant is ready to use!"
echo ""
echo "To start the voice assistant, run:"
echo "  ./run-voice.sh"
echo ""
echo "Then say 'hey komputa' to activate it."
echo ""
