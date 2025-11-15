#!/bin/bash

echo ""
echo "==============================================="
echo "  Komputa API Key Setup"
echo "==============================================="
echo ""
echo "This script will help you set up your OpenAI API key."
echo ""
echo "You can get an API key from:"
echo "  https://platform.openai.com/api-keys"
echo ""
echo ""

read -p "Enter your OpenAI API Key: " APIKEY

if [ -z "$APIKEY" ]; then
    echo ""
    echo "Error: No API key provided!"
    echo ""
    exit 1
fi

echo ""
echo "Setting up API key for Web Interface..."
cd src/Komputa.Presentation.WebAPI
dotnet user-secrets set "OpenAI:ApiKey" "$APIKEY"

echo ""
echo "Setting up API key for Console Interface..."
cd ../Komputa.Presentation.Console
dotnet user-secrets set "OpenAI:ApiKey" "$APIKEY"

cd ../..

echo ""
echo "==============================================="
echo "  Setup Complete!"
echo "==============================================="
echo ""
echo "Your API key has been securely stored."
echo "You can now run the chat bot using:"
echo "  - ./run-web.sh     (for web interface)"
echo "  - ./run-console.sh (for console interface)"
echo ""
