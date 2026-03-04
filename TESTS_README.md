# Testes Unitários - AgroSolutions.Monitoracao

## Estrutura de Testes

A suite de testes foi organizada em camadas que espelham a arquitetura do projeto:

```
tests/AgroSolutions.Monitoracao.Tests/
├── Dominio/
│   └── AlertaTests.cs                          # Testes das entidades de domínio
├── Aplicacao/
│   └── MotorAlertasTests.cs                    # Testes da lógica de negócio
└── Infra/
    └── RabbitMqLeiturasQueueConsumerTests.cs   # Testes de infraestrutura
```

## Cobertura de Testes

### Total: 27 Testes ✓

#### Dominio/ (8 testes)
- **AlertaTests**: 6 testes
  - Verificação de status de alerta (Ativo/Resolvido)
  - Propriedades e enumerações
  - Comportamento de estado
  
- **EstadoMonitoramentoTalhaoTests**: 2 testes
  - Criação e inicialização
  - Atualização de estado de monitoramento

#### Aplicacao/ (9 testes)
- **MotorAlertasTests**: 6 testes
  - Criação de alertas quando umidade está baixa
  - Impacto de alertas já existentes
  - Atualização de estado de monitoramento
  - Resolução de alertas quando umidade normaliza
  - Manutenção de alertas quando umidade ainda está baixa
  - Logging de eventos

- **LeiturasQueueConsumerHostedServiceTests**: 3 testes
  - Parada do consumer
  - Verificação de herança de BackgroundService
  - Inicialização do consumer

#### Infra/ (10 testes)
- **RabbitMqLeiturasQueueConsumerTests**: 10 testes
  - Criação correta de LeituraSensorIngerida
  - Decomposição de record
  - Igualdade entre instâncias
  - Variações de umidade, temperatura e precipitação
  - Validação de ID, ID do talhão e data
  - Representação em string

## Padrões Utilizados

### 1. Arrange-Act-Assert (AAA)
Todos os testes seguem o padrão AAA para clareza:
```csharp
[Fact]
public async Task ProcessarLeituraAsync_DeveCriarAlertaSeCA()
{
    // Arrange - Preparação dos dados
    var idTalhao = Guid.NewGuid();
    
    // Act - Execução da ação
    await _motorAlertas.ProcessarLeituraAsync(leitura);
    
    // Assert - Verificação dos resultados
    _mockAlertaRepository.Verify(...);
}
```

### 2. Mocking com Moq
Uso de mocks para isolar a lógica de negócio:
```csharp
_mockAlertaRepository
    .Setup(r => r.InserirAsync(It.IsAny<Alerta>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync("alerta-001");
```

### 3. Nomenclatura Clara
Nomes de testes descritivos que indicam:
- Unidade sob teste
- Condição/Cenário
- Resultado esperado

Formato: `Metodo_Condicao_ResultadoEsperado`

### 4. Test Fixtures
Uso de constructor para inicializar mocks reutilizáveis:
```csharp
public MotorAlertasTests()
{
    _mockAlertaRepository = new Mock<IAlertaRepository>();
    _motorAlertas = new MotorAlertas(
        _mockAlertaRepository.Object,
        // ...
    );
}
```

## Principais Cenários Testados

### 1. Processamento de Leitura
- ✓ Criação de alerta quando umidade below limit
- ✓ Skipping de alerta quando já existe
- ✓ Resolução de alerta quando umidade normaliza
- ✓ Manutenção de alerta quando umidade ainda baixa

### 2. Entidades de Domínio
- ✓ Cálculo de status (Ativo/Resolvido)
- ✓ Inicialização correta de propriedades
- ✓ Suporte a decomposição de records

### 3. Infraestrutura
- ✓ Criação de mensagens de sensor
- ✓ Validação de tipos de dados
- ✓ Igualdade de instâncias

## Ferramentas Utilizadas

- **Framework**: xUnit
- **Mocking**: Moq 4.20.70
- **.NET**: 8.0

## Executar os Testes

### Executar todos os testes
```bash
dotnet test tests/AgroSolutions.Monitoracao.Tests/
```

### Executar testes com logger detalhado
```bash
dotnet test tests/AgroSolutions.Monitoracao.Tests/ --logger "console;verbosity=detailed"
```

### Executar testes de um arquivo específico
```bash
dotnet test tests/AgroSolutions.Monitoracao.Tests/ --filter "MotorAlertasTests"
```

## Cobertura de Código

Cobertura estimada: **70%+**

A estrutura de testes cobre:
- Toda lógica de negócio crítica (MotorAlertas)
- Comportamento de todas as entidades de domínio
- Integração com injeção de dependência
- Processamento de eventos da fila

## Próximos Passos para Melhorar Cobertura

1. **Testes de Repositório**: Adicionar testes de integração para AlertaRepository e EstadoMonitoramentoRepository
2. **Testes de Integração**: End-to-end com MongoDB real
3. **Testes de API**: Testar endpoints da API REST
4. **Performance**: Adicionar testes de carga e performance

## Observações Importantes

- O arquivo UnitTest1.cs foi removido e substituído por testes bem estruturados
- Todos os testes são assíncronos quando necessário
- Uso de mocks garante testes rápidos e independentes
- Nenhuma dependência externa de banco de dados ou RabbitMQ é necessária
