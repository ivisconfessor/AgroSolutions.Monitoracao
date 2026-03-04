using AgroSolutions.Monitoracao.Infra;
using Xunit;

namespace AgroSolutions.Monitoracao.Tests.Infra;

public class RabbitMqLeiturasQueueConsumerTests
{
    [Fact]
    public void LeituraSensorIngerida_DeveSerCriadaComTodasAsPropriedades()
    {
        // Arrange & Act
        var idTalhao = Guid.NewGuid();
        var dataLeitura = DateTimeOffset.UtcNow;
        var leitura = new LeituraSensorIngerida(
            "leitura-001",
            idTalhao,
            dataLeitura,
            55.0,
            28.5,
            2.5
        );

        // Assert
        Assert.Equal("leitura-001", leitura.Id);
        Assert.Equal(idTalhao, leitura.IdTalhao);
        Assert.Equal(dataLeitura, leitura.DataLeitura);
        Assert.Equal(55.0, leitura.UmidadeSolo);
        Assert.Equal(28.5, leitura.Temperatura);
        Assert.Equal(2.5, leitura.Precipitacao);
    }

    [Fact]
    public void LeituraSensorIngerida_DeveSerDecompostoCorretamente()
    {
        // Arrange
        var leitura = new LeituraSensorIngerida(
            "leitura-002",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            60.0,
            25.0,
            0.0
        );

        // Act
        var (id, idTalhao, dataLeitura, umidade, temp, precip) = leitura;

        // Assert
        Assert.Equal("leitura-002", id);
        Assert.Equal(60.0, umidade);
        Assert.Equal(25.0, temp);
        Assert.Equal(0.0, precip);
    }

    [Fact]
    public void LeituraSensorIngerida_DeveSuportarEquality()
    {
        // Arrange
        var idTalhao = Guid.NewGuid();
        var dataLeitura = DateTimeOffset.UtcNow;

        var leitura1 = new LeituraSensorIngerida(
            "leitura-001",
            idTalhao,
            dataLeitura,
            50.0,
            30.0,
            1.0
        );

        var leitura2 = new LeituraSensorIngerida(
            "leitura-001",
            idTalhao,
            dataLeitura,
            50.0,
            30.0,
            1.0
        );

        var leitura3 = new LeituraSensorIngerida(
            "leitura-002",
            Guid.NewGuid(),
            dataLeitura,
            50.0,
            30.0,
            1.0
        );

        // Assert
        Assert.Equal(leitura1, leitura2);
        Assert.NotEqual(leitura1, leitura3);
    }

    [Fact]
    public void LeituraSensorIngerida_DeveSuportarVariacoesUmidade()
    {
        // Arrange
        var umidades = new double[] { 0.0, 30.0, 50.0, 75.0, 100.0 };

        // Act & Assert
        foreach (var umidade in umidades)
        {
            var leitura = new LeituraSensorIngerida(
                Guid.NewGuid().ToString(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                umidade,
                25.0,
                0.0
            );

            Assert.Equal(umidade, leitura.UmidadeSolo);
        }
    }

    [Fact]
    public void LeituraSensorIngerida_DeveSuportarVariacoesTemperatura()
    {
        // Arrange
        var temperaturas = new double[] { -10.0, 0.0, 25.0, 40.0, 50.0 };

        // Act & Assert
        foreach (var temperatura in temperaturas)
        {
            var leitura = new LeituraSensorIngerida(
                Guid.NewGuid().ToString(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                50.0,
                temperatura,
                0.0
            );

            Assert.Equal(temperatura, leitura.Temperatura);
        }
    }

    [Fact]
    public void LeituraSensorIngerida_DeveSuportarPrecipitacaoNegativa()
    {
        // Arrange & Act
        var leitura = new LeituraSensorIngerida(
            "leitura-003",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            50.0,
            25.0,
            -5.0
        );

        // Assert
        Assert.Equal(-5.0, leitura.Precipitacao);
    }

    [Fact]
    public void LeituraSensorIngerida_DeveConterIdNaoVazio()
    {
        // Arrange
        var leitura = new LeituraSensorIngerida(
            "test-id-123",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            50.0,
            25.0,
            0.0
        );

        // Assert
        Assert.NotEmpty(leitura.Id);
        Assert.Equal("test-id-123", leitura.Id);
    }

    [Fact]
    public void LeituraSensorIngerida_DeveConterIdTalhaoValido()
    {
        // Arrange
        var idTalhao = Guid.NewGuid();
        var leitura = new LeituraSensorIngerida(
            "leitura-004",
            idTalhao,
            DateTimeOffset.UtcNow,
            50.0,
            25.0,
            0.0
        );

        // Assert
        Assert.NotEqual(Guid.Empty, leitura.IdTalhao);
        Assert.Equal(idTalhao, leitura.IdTalhao);
    }

    [Fact]
    public void LeituraSensorIngerida_DeveConterDataLeituraValida()
    {
        // Arrange
        var dataEsperada = DateTimeOffset.UtcNow;
        var leitura = new LeituraSensorIngerida(
            "leitura-005",
            Guid.NewGuid(),
            dataEsperada,
            50.0,
            25.0,
            0.0
        );

        // Assert
        Assert.Equal(dataEsperada, leitura.DataLeitura);
        Assert.NotEqual(DateTimeOffset.MinValue, leitura.DataLeitura);
    }

    [Fact]
    public void LeituraSensorIngerida_DeveRetornarStringRepresentacao()
    {
        // Arrange
        var leitura = new LeituraSensorIngerida(
            "leitura-006",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            50.0,
            25.0,
            0.0
        );

        // Act
        var stringRep = leitura.ToString();

        // Assert
        Assert.NotEmpty(stringRep);
        Assert.Contains("leitura-006", stringRep);
    }
}
