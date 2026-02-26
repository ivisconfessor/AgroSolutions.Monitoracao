# AgroSolutions.Monitoracao

Microserviço de **motor de alertas** do AgroSolutions. Processa leituras de sensores e gera alertas automáticos quando condições críticas são atingidas.

## Funcionalidades

- **Motor de Alertas**: Aplica regras de monitoramento sobre dados de sensores
- **Alerta de Seca**: Detecta quando umidade do solo fica abaixo de 30% por mais de 24 horas
- **Consumer RabbitMQ**: Escuta fila `agrosolutions.sensores.leituras` de forma contínua
- **Persistência MongoDB**: Armazena alertas gerados e estado de monitoramento

## Estrutura

- **Api**: Minimal APIs (GET/POST alertas) + Consumer hospedado _background service_
- **Aplicacao**: `MotorAlertas` (lógica de regras) + `LeiturasQueueConsumerHostedService` (orquestração)
- **Dominio**: Entidades `Alerta`, `EstadoMonitoramentoTalhao`, enumeradores
- **Infra**: MongoDB (repositórios) + RabbitMQ (consumer)

## MongoDB (Mongo Atlas)

### Collections

Crie no Mongo Atlas um database (ex.: `agrosolutions_monitoracao`) com as collections:

| Collection | Uso |
|---|---|
| **alertas** | Um alerta por documento: talhão, tipo (ex.: seca), mensagem, datas. |
| **monitoramento_talhoes** | Estado mínimo por talhão: "seco desde", última leitura, última umidade. |

### Índices recomendados

Na collection `alertas`:
- **Campos**: `id_talhao` (asc), `criado_em` (desc)
- **Nome**: `ix_alertas_id_talhao_criado`

Na collection `monitoramento_talhoes`:
- **Campo**: `id_talhao` (id único do talhão)

## RabbitMQ

- **Fila**: `agrosolutions.sensores.leituras` (mesmo do `AgroSolutions.Sensores`)
- O consumer escuta continuamente e processa cada leitura com o motor de alertas
- Ack manual: sucesso = ack, erro = nack com requeue

## Configuração

Em `appsettings.json` ou **User Secrets** (recomendado em dev):

```json
{
  "MongoDb": {
    "ConnectionString": "<sua connection string do Mongo Atlas>",
    "DatabaseName": "agrosolutions_monitoracao"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest",
    "FilaLeituras": "agrosolutions.sensores.leituras"
  }
}
```

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | /alertas/{id} | Obter alerta por id |
| GET | /alertas?idTalhao={guid}&somenteAtivos={bool}&limite={int} | Listar alertas por talhão |
| POST | /alertas/{id}/resolver | Resolver um alerta (marcar como resolvido) |

## Swagger

Acesse: `https://localhost:<port>/swagger`

## Motor de Alertas

### Regra: Alerta de Seca

1. **Condição**: Umidade do solo **< 30%**
2. **Duração mínima**: > 24 horas na mesma condição
3. **Ação**: Gera novo alerta (se não existir ativo) ou aguarda resolução
4. **Resolução**: Quando umidade sobe acima de 30%, alerta é marcado como resolvido

### Fluxo

```
Leitura de Sensor → Verifica Estado do Talhão → Aplica Regra de Seca
  → Atualiza Estado → Se alerta gerado, persiste em DB
```

## Executar Localmente

### Pré-requisitos
- .NET 8.0 SDK
- MongoDB (local ou Atlas)
- RabbitMQ (local ou gerenciado)
- `AgroSolutions.Sensores` ativo (publicando leituras na fila)

### Iniciar

```bash
# Restaurar dependências
dotnet restore

# Executar (porta padrão 5094 em desenvolvimento)
dotnet run --project src/AgroSolutions.Monitoracao.Api
```

Swagger estará em: `https://localhost:<port>/swagger`

## Docker

Imagem disponível em `./dockerfile`. E.g.:

```bash
docker build -t agrosolutions-monitoracao:latest .
docker run -p 5094:8080 -e MongoDb__ConnectionString="<mongo-uri>" \
  -e RabbitMQ__HostName="<rabbit-host>" agrosolutions-monitoracao:latest
```

## Testes

```bash
dotnet test tests/AgroSolutions.Monitoracao.Tests
```

## Integração com Dashboard

Os alertas são consultáveis via API REST e podem ser exibidos no dashboard se integrado ao `AgroSolutions.Usuario.API` ou UI frontend.

---

**[HACKATON FIAP 2024]**
