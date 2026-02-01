#!/bin/bash

# Kill all background processes on exit
trap "kill 0" EXIT

echo "🚀 Starting HealthyCron Development Environment..."

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
    echo "📦 node_modules not found, installing dependencies..."
    npm install
fi

# Start Tailwind CSS watcher in the background
echo "🎨 Starting Tailwind CSS watcher..."
npm run watch:css &

# Start .NET watcher
echo "🔥 Starting .NET watcher..."
dotnet watch run
