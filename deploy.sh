#!/bin/bash

# ReviewHub Production Deployment Script
# This script deploys the ReviewHub application to production

set -e  # Exit on error

echo "🚀 ReviewHub Production Deployment"
echo "=================================="

# Check if .env.production exists
if [ ! -f .env.production ]; then
    echo "❌ Error: .env.production file not found!"
    echo "Please create .env.production from .env.production.example"
    exit 1
fi

# Load environment variables
export $(cat .env.production | grep -v '^#' | xargs)

echo "✅ Environment variables loaded"

# Step 1: Pull latest code (if using git)
echo ""
echo "📥 Pulling latest code..."
git pull origin main

# Step 2: Build Docker images
echo ""
echo "🔨 Building Docker images..."
docker-compose build --no-cache

# Step 3: Stop existing containers
echo ""
echo "🛑 Stopping existing containers..."
docker-compose down

# Step 4: Start new containers
echo ""
echo "▶️  Starting new containers..."
docker-compose up -d

# Step 5: Wait for database to be ready
echo ""
echo "⏳ Waiting for database to be ready..."
sleep 10

# Step 6: Run database migrations
echo ""
echo "📊 Running database migrations..."
docker-compose exec -T api dotnet ef database update --project /src/ReviewHub.API

# Step 7: Check health
echo ""
echo "🏥 Checking application health..."
sleep 5

API_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/health)
CLIENT_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:3000/health)

if [ "$API_HEALTH" = "200" ]; then
    echo "✅ API is healthy"
else
    echo "❌ API health check failed (HTTP $API_HEALTH)"
    exit 1
fi

if [ "$CLIENT_HEALTH" = "200" ]; then
    echo "✅ Client is healthy"
else
    echo "❌ Client health check failed (HTTP $CLIENT_HEALTH)"
    exit 1
fi

# Step 8: Show running containers
echo ""
echo "📋 Running containers:"
docker-compose ps

echo ""
echo "✅ Deployment completed successfully!"
echo ""
echo "🌐 Application URLs:"
echo "   API: http://localhost:5000"
echo "   Client: http://localhost:3000"
echo "   Database: localhost:1433"
echo ""
echo "📝 To view logs: docker-compose logs -f"
echo "🛑 To stop: docker-compose down"
