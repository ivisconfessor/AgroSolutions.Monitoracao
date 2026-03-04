using AgroSolutions.Monitoracao.Aplicacao;
using AgroSolutions.Monitoracao.Dominio;
using AgroSolutions.Monitoracao.Infra;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AgroSolutions.Monitoracao.Tests.Aplicacao;

public class MotorAlertasTests
{
    private readonly Mock<IAlertaRepository> _mockAlertaRepository;
    private readonly Mock<IEstadoMonitoramentoRepository> _mockEstadoRepository;
    private readonly Mock<ILogger<MotorAlertas>> _mockLogger;
    private readonly MotorAlertas _motorAlertas;

    public MotorAlertasTests()
    {
        _mockAlertaRepository = new Mock<IAlertaRepository>();
        _mockEstadoRepository = new Mock<IEstadoMonitoramentoRepository>();
        _mockLogger = new Mock<ILogger<MotorAlertas>>();
        
        _motorAlertas = new MotorAlertas(
            _mockAlertaRepository.Object,
            _mockEstadoRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ProcessarLeituraAsync_DeveCriarAlertaSecaQuandoUmidadeAbaixoDoLimiteSemAlertaAtivo()
    {
        // Arrange
        var idTalhao = Guid.NewGuid();
        var dataLeitura = DateTimeOffset.UtcNow;
        var leitura = new LeituraSensorIngerida(
            "leitura-001",
            idTalhao,
            dataLeitura,
            50.0,  // Abaixo do limite 60%
            30.0,
            0.0
        );

        _mockEstadoRepository
            .Setup(r => r.ObterAsync(idTalhao, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EstadoMonitoramentoTalhao)null!);

        _mockAlertaRepository
            .Setup(r => r.ObterAtivoPorTalhaoETipoAsync(idTalhao, TipoAlerta.Seca, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alerta)null!);

        _mockAlertaRepository
            .Setup(r => r.InserirAsync(It.IsAny<Alerta>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("alerta-001");

        // Act
        await _motorAlertas.ProcessarLeituraAsync(leitura);

        // Assert
        _mockAlertaRepository.Verify(
            r => r.InserirAsync(It.Is<Alerta>(a =>
                a.IdTalhao == idTalhao &&
                a.Tipo == TipoAlerta.Seca),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_NaoDeveraCriarAlertaQuandoJaExistiAlertaAtivo()
    {
        // Arrange
        var idTalhao = Guid.NewGuid();
        var dataLeitura = DateTimeOffset.UtcNow;
        var leitura = new LeituraSensorIngerida(
            "leitura-002",
            idTalhao,
            dataLeitura,
            50.0,  // Abaixo do limite
            30.0,
            0.0
        );

        var estadoExistente = new EstadoMonitoramentoTalhao
        {
            IdTalhao = idTalhao,
            SecoDesde = dataLeitura.AddHours(-2),
            UltimaLeituraEm = dataLeitura.AddHours(-1),
            UltimaUmidadeSolo = 55.0
        };

        var alertaExistente = new Alerta
        {
            Id = "alerta-existente",
            IdTalhao = idTalhao,
            Tipo = TipoAlerta.Seca,
            Mensagem = "Alerta de seca",
            CriadoEm = dataLeitura.AddHours(-2),
            ResolvidoEm = null
        };

        _mockEstadoRepository
            .Setup(r => r.ObterAsync(idTalhao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estadoExistente);

        _mockAlertaRepository
            .Setup(r => r.ObterAtivoPorTalhaoETipoAsync(idTalhao, TipoAlerta.Seca, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alertaExistente);

        // Act
        await _motorAlertas.ProcessarLeituraAsync(leitura);

        // Assert
        _mockAlertaRepository.Verify(
            r => r.InserirAsync(It.IsAny<Alerta>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_DeveAtualizarEstadoMonitoramentoComUltimosValores()
    {
        // Arrange
        var idTalhao = Guid.NewGuid();
        var dataLeitura = DateTimeOffset.UtcNow;
        var leitura = new LeituraSensorIngerida(
            "leitura-003",
            idTalhao,
            dataLeitura,
            75.0,  // Acima do limite
            28.0,
            5.0
        );

        var estadoExistente = new EstadoMonitoramentoTalhao
        {
            IdTalhao = idTalhao,
            SecoDesde = null,
            UltimaLeituraEm = dataLeitura.AddHours(-1),
            UltimaUmidadeSolo = 70.0
        };

        _mockEstadoRepository
            .Setup(r => r.ObterAsync(idTalhao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estadoExistente);

        _mockAlertaRepository
            .Setup(r => r.ObterAtivoPorTalhaoETipoAsync(idTalhao, TipoAlerta.Seca, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alerta)null!);

        // Act
        await _motorAlertas.ProcessarLeituraAsync(leitura);

        // Assert
        _mockEstadoRepository.Verify(
            r => r.UpsertAsync(It.Is<EstadoMonitoramentoTalhao>(e =>
                e.IdTalhao == idTalhao &&
                e.UltimaUmidadeSolo == 75.0 &&
                e.UltimaLeituraEm == dataLeitura),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_DeveResolverAlertaQuandoUmidadeVoltaAoNormal()
    {
        // Arrange
        var idTalhao = Guid.NewGuid();
        var dataLeitura = DateTimeOffset.UtcNow;

        var leitura = new LeituraSensorIngerida(
            "leitura-004",
            idTalhao,
            dataLeitura,
            75.0,  // Acima do limite
            25.0,
            10.0
        );

        var estadoComSeca = new EstadoMonitoramentoTalhao
        {
            IdTalhao = idTalhao,
            SecoDesde = dataLeitura.AddHours(-10),
            UltimaLeituraEm = dataLeitura.AddHours(-1),
            UltimaUmidadeSolo = 45.0  // Umidade baixa anterior
        };

        var alertaAtivo = new Alerta
        {
            Id = "alerta-abc123",
            IdTalhao = idTalhao,
            Tipo = TipoAlerta.Seca,
            Mensagem = "Alerta de seca",
            CriadoEm = dataLeitura.AddHours(-10),
            ResolvidoEm = null
        };

        _mockEstadoRepository
            .Setup(r => r.ObterAsync(idTalhao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estadoComSeca);

        _mockAlertaRepository
            .Setup(r => r.ObterAtivoPorTalhaoETipoAsync(idTalhao, TipoAlerta.Seca, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alertaAtivo);

        _mockAlertaRepository
            .Setup(r => r.ResolverAsync(alertaAtivo.Id, dataLeitura, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _motorAlertas.ProcessarLeituraAsync(leitura);

        // Assert
        _mockAlertaRepository.Verify(
            r => r.ResolverAsync("alerta-abc123", dataLeitura, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockEstadoRepository.Verify(
            r => r.UpsertAsync(It.Is<EstadoMonitoramentoTalhao>(e =>
                e.IdTalhao == idTalhao &&
                e.SecoDesde == null),  // SecoDesde foi resetado
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_NaoDeveResolverAlertaQuandoUmidadeAindaBaixa()
    {
        // Arrange
        var idTalhao = Guid.NewGuid();
        var dataLeitura = DateTimeOffset.UtcNow;

        var leitura = new LeituraSensorIngerida(
            "leitura-005",
            idTalhao,
            dataLeitura,
            55.0,  // Ainda abaixo do limite
            30.0,
            0.0
        );

        var estadoComSeca = new EstadoMonitoramentoTalhao
        {
            IdTalhao = idTalhao,
            SecoDesde = dataLeitura.AddHours(-10),
            UltimaLeituraEm = dataLeitura.AddHours(-1),
            UltimaUmidadeSolo = 50.0
        };

        var alertaAtivo = new Alerta
        {
            Id = "alerta-xyz789",
            IdTalhao = idTalhao,
            Tipo = TipoAlerta.Seca,
            Mensagem = "Alerta de seca",
            CriadoEm = dataLeitura.AddHours(-10),
            ResolvidoEm = null
        };

        _mockEstadoRepository
            .Setup(r => r.ObterAsync(idTalhao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estadoComSeca);

        _mockAlertaRepository
            .Setup(r => r.ObterAtivoPorTalhaoETipoAsync(idTalhao, TipoAlerta.Seca, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alertaAtivo);

        // Act
        await _motorAlertas.ProcessarLeituraAsync(leitura);

        // Assert
        _mockAlertaRepository.Verify(
            r => r.ResolverAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockEstadoRepository.Verify(
            r => r.UpsertAsync(It.Is<EstadoMonitoramentoTalhao>(e =>
                e.IdTalhao == idTalhao &&
                e.SecoDesde == dataLeitura.AddHours(-10)),  // SecoDesde mantido
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarLeituraAsync_DeveLogInfoQuandoCriarAlerta()
    {
        // Arrange
        var idTalhao = Guid.NewGuid();
        var dataLeitura = DateTimeOffset.UtcNow;
        var leitura = new LeituraSensorIngerida(
            "leitura-006",
            idTalhao,
            dataLeitura,
            50.0,
            30.0,
            0.0
        );

        _mockEstadoRepository
            .Setup(r => r.ObterAsync(idTalhao, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EstadoMonitoramentoTalhao)null!);

        _mockAlertaRepository
            .Setup(r => r.ObterAtivoPorTalhaoETipoAsync(idTalhao, TipoAlerta.Seca, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alerta)null!);

        _mockAlertaRepository
            .Setup(r => r.InserirAsync(It.IsAny<Alerta>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("alerta-novo");

        // Act
        await _motorAlertas.ProcessarLeituraAsync(leitura);

        // Assert - Verificar que logs foram chamados
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Criando novo alerta")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

public class LeiturasQueueConsumerHostedServiceTests
{
    private readonly Mock<ILeiturasQueueConsumer> _mockConsumer;
    private readonly Mock<IMotorAlertas> _mockMotorAlertas;
    private readonly Mock<ILogger<LeiturasQueueConsumerHostedService>> _mockLogger;
    private readonly LeiturasQueueConsumerHostedService _service;

    public LeiturasQueueConsumerHostedServiceTests()
    {
        _mockConsumer = new Mock<ILeiturasQueueConsumer>();
        _mockMotorAlertas = new Mock<IMotorAlertas>();
        _mockLogger = new Mock<ILogger<LeiturasQueueConsumerHostedService>>();

        _service = new LeiturasQueueConsumerHostedService(
            _mockConsumer.Object,
            _mockMotorAlertas.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task StopAsync_DevePararoConsumerQuandoServicoEhParado()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        _mockConsumer
            .Setup(c => c.StopAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _service.StopAsync(cts.Token);

        // Assert
        _mockConsumer.Verify(
            c => c.StopAsync(),
            Times.Once);
    }

    [Fact]
    public void LeiturasQueueConsumerHostedService_DeveImplementarBackgroundService()
    {
        // Assert
        var baseType = typeof(LeiturasQueueConsumerHostedService).BaseType;
        Assert.NotNull(baseType);
        Assert.Contains("BackgroundService", baseType.Name);
    }

    [Fact]
    public async Task StartAsync_DeveInicializarConsumer()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        _mockConsumer
            .Setup(c => c.StartAsync(It.IsAny<Func<LeituraSensorIngerida, Task>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        try
        {
            await _service.StartAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Esperado - o serviço foi cancelado
        }

        // O Start deve ter chamado o consumer
        _mockConsumer.Verify(
            c => c.StartAsync(It.IsAny<Func<LeituraSensorIngerida, Task>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
