#!/bin/bash

echo "Setting up RSS Feed Application..."

# Build and run with Docker Compose
echo "Building and starting services..."
docker-compose up -d

echo "Waiting for PostgreSQL to be ready..."
sleep 10

echo "Running the RSS feed application..."
docker-compose run rss-app

echo "Cleaning up..."
docker-compose down

echo "Done!"