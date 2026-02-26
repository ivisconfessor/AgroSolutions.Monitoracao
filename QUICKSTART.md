# QUICK START - AgroSolutions.Monitoracao

## Pré-requisitos

- .NET 8.0 SDK
- Docker & Docker Compose
- Git

## Opção 1: Executar com Docker Compose (Recomendado)

```bash
# 1. Clone e entre no diretório
cd AgroSolutions.Monitoracao

# 2. Inicie tudo (MongoDB + RabbitMQ + API)
docker-compose up

# 3. Acesse
# - API Swagger: http://localhost:5094/swagger
# - RabbitMQ: http://localhost:15672 (guest:guest)
```

## Opção 2: Executar Localmente

```bash
# 1. Inicie MongoDB (ou use Atlas)
mongod

# 2. Inicie RabbitMQ
docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# 3. Configure User Secrets (opcional)
dotnet user-secrets set MongoDb:ConnectionString "mongodb://localhost:27017"
dotnet user-secrets set RabbitMQ:HostName "localhost"

# 4. Compile e execute
dotnet build
dotnet run --project src/AgroSolutions.Monitoracao.Api
```

Swagger estará em: `http://localhost:5000/swagger` (ou porta configurada)

## Testando a API

### 1. Listar alertas por talhão

```bash
curl -X GET "http://localhost:5094/alertas?idTalhao=550e8400-e29b-41d4-a716-446655440000&somenteAtivos=true&limite=10" \
  -H "accept: application/json"
```

### 2. Obter alerta específico

```bash
curl -X GET "http://localhost:5094/alertas/{id}" \
  -H "accept: application/json"
```

### 3. Resolver alerta

```bash
curl -X POST "http://localhost:5094/alertas/{id}/resolver" \
  -H "accept: application/json"
```

## Fluxo End-to-End

1. **AgroSolutions.Sensores** publica leitura na fila `agrosolutions.sensores.leituras`
2. **AgroSolutions.Monitoracao** consome a mensagem
3. **MotorAlertas** aplica regras (verificação de seca, etc)
4. **Alerta gerado** persiste em MongoDB se condição atendida
5. **Dashboard** consulta `/alertas` endpoint para exibir ao produtor

## Monitoramento

### Logs

```bash
# Ver logs do container
docker logs -f agrosolutions-monitoracao-api

# Ou na execução local
dotnet run --project src/AgroSolutions.Monitoracao.Api
```

### MongoDB

```bash
# Conectar ao container
docker exec -it agrosolutions-mongo mongosh

# Ver alertas
use agrosolutions_monitoracao
db.alertas.find()

# Ver estado de monitoramento
db.monitoramento_talhoes.find()
```

### RabbitMQ

Acesse http://localhost:15672 (guest:guest) para:
- Ver fila `agrosolutions.sensores.leituras`
- Monitorar conexões e throughput

## Parar Serviços

```bash
docker-compose down    # Para e remove containers
docker-compose down -v # Remove também volumes de dados
```

## Troubleshooting

### Erro: "MongoDb:ConnectionString não está configurada"

→ Defina em `appsettings.json` ou variáveis de ambiente

### Erro: "Connection refused" no RabbitMQ

→ Aguarde o RabbitMQ inicializar (~30 segundos)

### Erro: "Failed to connect to MongoDB"

→ Verifique se MongoDB está rodando e acessível na porta 27017

---

Para mais detalhes, veja [README.md](./README.md)
