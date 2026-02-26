# AeroSolutions.Monitoracao - User Secrets Setup

Para desenvolvimento local, configure User Secrets:

## 1. Inicialize User Secrets (se não estiver inicializado)

```bash
cd src/AgroSolutions.Monitoracao.Api
dotnet user-secrets init
```

## 2. Configure as variáveis

### MongoDB (Local)
```bash
dotnet user-secrets set "MongoDb:ConnectionString" "mongodb://localhost:27017"
dotnet user-secrets set "MongoDb:DatabaseName" "agrosolutions_monitoracao"
```

### MongoDB (Atlas)
```bash
dotnet user-secrets set "MongoDb:ConnectionString" "mongodb+srv://<username>:<password>@<cluster>.mongodb.net/?retryWrites=true&w=majority"
dotnet user-secrets set "MongoDb:DatabaseName" "agrosolutions_monitoracao"
```

### RabbitMQ (Local)
```bash
dotnet user-secrets set "RabbitMQ:HostName" "localhost"
dotnet user-secrets set "RabbitMQ:UserName" "guest"
dotnet user-secrets set "RabbitMQ:Password" "guest"
dotnet user-secrets set "RabbitMQ:FilaLeituras" "agrosolutions.sensores.leituras"
```

### RabbitMQ (CloudAMQP ou similar)
```bash
dotnet user-secrets set "RabbitMQ:HostName" "your-rabbitmq-host.cloudamqp.com"
dotnet user-secrets set "RabbitMQ:UserName" "your-username"
dotnet user-secrets set "RabbitMQ:Password" "your-password"
dotnet user-secrets set "RabbitMQ:FilaLeituras" "agrosolutions.sensores.leituras"
```

## 3. Verificar Configurações

```bash
dotnet user-secrets list
```

User Secrets no macOS/Linux ficam em: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

---

Nota: User Secrets NÃO devem ser comitados no git. Use apenas para desenvolvimento local.
Para produção, use variáveis de ambiente ou Azure Key Vault.
