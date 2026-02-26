using AgroSolutions.Monitoracao.Infra;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgroSolutions.Monitoracao.Aplicacao;

/// <summary>
/// Serviço hospedado que executa continuamente o consumer de fila RabbitMQ
/// processando leituras de sensores.
/// </summary>
public class LeiturasQueueConsumerHostedService : BackgroundService
{
    private readonly ILeiturasQueueConsumer _consumer;
    private readonly IMotorAlertas _motorAlertas;
    private readonly ILogger<LeiturasQueueConsumerHostedService> _logger;

    public LeiturasQueueConsumerHostedService(
        ILeiturasQueueConsumer consumer,
        IMotorAlertas motorAlertas,
        ILogger<LeiturasQueueConsumerHostedService> logger)
    {
        _consumer = consumer;
        _motorAlertas = motorAlertas;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando consumer de leituras de sensores...");

        try
        {
            await _consumer.StartAsync(ProcessarLeitura, stoppingToken);

            // Mantém o serviço rodando enquanto a aplicação estiver em execução
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer de leituras cancelado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no consumer de leituras de sensores");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parando consumer de leituras de sensores...");
        await _consumer.StopAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessarLeitura(LeituraSensorIngerida leitura)
    {
        try
        {
            _logger.LogDebug($"Processando leitura: Talhão {leitura.IdTalhao}, Umidade {leitura.UmidadeSolo}%");
            await _motorAlertas.ProcessarLeituraAsync(leitura);
            _logger.LogDebug("Leitura processada com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao processar leitura do talhão {leitura.IdTalhao}");
        }
    }
}
