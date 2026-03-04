using AgroSolutions.Monitoracao.Dominio;
using Xunit;

namespace AgroSolutions.Monitoracao.Tests.Dominio;

public class AlertaTests
{
    [Fact]
    public void Alerta_DeveTerStatusAtivoQuandoResolvidoEmNull()
    {
        // Arrange
        var alerta = new Alerta
        {
            Id = "alerta-123",
            IdTalhao = Guid.NewGuid(),
            Tipo = TipoAlerta.Seca,
            Mensagem = "Teste",
            CriadoEm = DateTimeOffset.UtcNow,
            ResolvidoEm = null
        };

        // Act
        var status = alerta.Status;

        // Assert
        Assert.Equal(StatusAlerta.Ativo, status);
    }

    [Fact]
    public void Alerta_DeveTerStatusResolvidoQuandoResolvidoEmTemValor()
    {
        // Arrange
        var agora = DateTimeOffset.UtcNow;
        var alerta = new Alerta
        {
            Id = "alerta-456",
            IdTalhao = Guid.NewGuid(),
            Tipo = TipoAlerta.Seca,
            Mensagem = "Teste",
            CriadoEm = agora,
            ResolvidoEm = agora.AddHours(1)
        };

        // Act
        var status = alerta.Status;

        // Assert
        Assert.Equal(StatusAlerta.Resolvido, status);
    }

    [Fact]
    public void Alerta_DeveSerCriandoComPropriedadesCorretas()
    {
        // Arrange & Act
        var idTalhao = Guid.NewGuid();
        var agora = DateTimeOffset.UtcNow;
        var alerta = new Alerta
        {
            Id = "alerta-789",
            IdTalhao = idTalhao,
            Tipo = TipoAlerta.Seca,
            Mensagem = "Mensagem de teste",
            CriadoEm = agora,
            ResolvidoEm = null
        };

        // Assert
        Assert.Equal("alerta-789", alerta.Id);
        Assert.Equal(idTalhao, alerta.IdTalhao);
        Assert.Equal(TipoAlerta.Seca, alerta.Tipo);
        Assert.Equal("Mensagem de teste", alerta.Mensagem);
        Assert.Equal(agora, alerta.CriadoEm);
        Assert.Null(alerta.ResolvidoEm);
    }

    [Fact]
    public void TipoAlerta_DeveConterValorSeca()
    {
        // Assert
        Assert.Equal(1, (int)TipoAlerta.Seca);
    }

    [Fact]
    public void StatusAlerta_DeveConterValoresAtivoeResolvido()
    {
        // Assert
        Assert.Equal(1, (int)StatusAlerta.Ativo);
        Assert.Equal(2, (int)StatusAlerta.Resolvido);
    }
}

public class EstadoMonitoramentoTalhaoTests
{
    [Fact]
    public void EstadoMonitoramentotalhao_DeveSerCriadoComPropriedadesCorretas()
    {
        // Arrange & Act
        var idTalhao = Guid.NewGuid();
        var ultimaLeitura = DateTimeOffset.UtcNow;
        var estado = new EstadoMonitoramentoTalhao
        {
            IdTalhao = idTalhao,
            SecoDesde = null,
            UltimaLeituraEm = ultimaLeitura,
            UltimaUmidadeSolo = 55.5
        };

        // Assert
        Assert.Equal(idTalhao, estado.IdTalhao);
        Assert.Null(estado.SecoDesde);
        Assert.Equal(ultimaLeitura, estado.UltimaLeituraEm);
        Assert.Equal(55.5, estado.UltimaUmidadeSolo);
    }

    [Fact]
    public void EstadoMonitoramentotalhao_DeveAtualizarSecoDesdeQuandoDefinido()
    {
        // Arrange
        var idTalhao = Guid.NewGuid();
        var secoDesde = DateTimeOffset.UtcNow;
        var estado = new EstadoMonitoramentoTalhao
        {
            IdTalhao = idTalhao,
            SecoDesde = null,
            UltimaLeituraEm = DateTimeOffset.UtcNow,
            UltimaUmidadeSolo = 50.0
        };

        // Act
        estado.SecoDesde = secoDesde;

        // Assert
        Assert.NotNull(estado.SecoDesde);
        Assert.Equal(secoDesde, estado.SecoDesde);
    }

    [Fact]
    public void EstadoMonitoramentotalhao_DeveAtualizarUltimosValores()
    {
        // Arrange
        var estado = new EstadoMonitoramentoTalhao
        {
            IdTalhao = Guid.NewGuid(),
            SecoDesde = null,
            UltimaLeituraEm = DateTimeOffset.UtcNow,
            UltimaUmidadeSolo = 70.0
        };

        var novaUltimaleitura = DateTimeOffset.UtcNow.AddMinutes(10);
        var novaUmidade = 65.5;

        // Act
        estado.UltimaLeituraEm = novaUltimaleitura;
        estado.UltimaUmidadeSolo = novaUmidade;

        // Assert
        Assert.Equal(novaUltimaleitura, estado.UltimaLeituraEm);
        Assert.Equal(novaUmidade, estado.UltimaUmidadeSolo);
    }
}
