#!/bin/bash

echo "Starting RSS Feed Application..."

# Make sure we're in the right directory
cd /C/fast_docker_projects/cursor/rss

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run

echo "Application finished."