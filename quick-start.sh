#!/bin/bash

# AgroSolutions.Monitoracao - Quick Start Script

echo "🚀 Iniciando AgroSolutions.Monitoracao"
echo ""

# Verificar Docker
if ! command -v docker &> /dev/null; then
    echo "❌ Docker não encontrado. Instale Docker primeiro."
    exit 1
fi

echo "1️⃣  Iniciando serviços (MongoDB + RabbitMQ)..."
docker-compose up -d

echo "2️⃣  Aguardando RabbitMQ estar pronto..."
sleep 10

echo "3️⃣  Compilando o projeto..."
dotnet build src/AgroSolutions.Monitoracao.Api/AgroSolutions.Monitoracao.Api.csproj

echo ""
echo "✅ Serviços iniciados!"
echo ""
echo "📊 URLs:"
echo "  - API Swagger: http://localhost:5000/swagger"
echo "  - MongoDB: localhost:27017"
echo "  - RabbitMQ Management: http://localhost:15672 (guest:guest)"
echo ""
echo "▶️  Iniciando API..."
dotnet run --project src/AgroSolutions.Monitoracao.Api
