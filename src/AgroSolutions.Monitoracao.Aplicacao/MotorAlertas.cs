using AgroSolutions.Monitoracao.Dominio;
using AgroSolutions.Monitoracao.Infra;
using Microsoft.Extensions.Logging;

namespace AgroSolutions.Monitoracao.Aplicacao;

/// <summary>
/// Aplica regras de monitoramento sobre leituras de sensores.
/// Verifica condições de alerta (ex.: seca) e cria alertas quando necessário.
/// </summary>
public interface IMotorAlertas
{
    Task ProcessarLeituraAsync(LeituraSensorIngerida leitura, CancellationToken ct = default);
}

public class MotorAlertas : IMotorAlertas
{
    private const double LIMITE_UMIDADE_SECA = 60.0;  // Reduzido para testes
    // private const int HORAS_MINIMAS_SECA = 24;    // Comentado para testes - criar alerta imediatamente

    private readonly IAlertaRepository _alertaRepository;
    private readonly IEstadoMonitoramentoRepository _estadoRepository;
    private readonly ILogger<MotorAlertas> _logger;

    public MotorAlertas(IAlertaRepository alertaRepository, IEstadoMonitoramentoRepository estadoRepository, ILogger<MotorAlertas> logger)
    {
        _alertaRepository = alertaRepository;
        _estadoRepository = estadoRepository;
        _logger = logger;
    }

    /// <summary>
    /// Processa uma leitura de sensor e aplica as regras de alerta.
    /// </summary>
    public async Task ProcessarLeituraAsync(LeituraSensorIngerida leitura, CancellationToken ct = default)
    {
        _logger.LogDebug("Processando leitura: Talhão {IdTalhao}, Umidade {UmidadeSolo}%", leitura.IdTalhao, leitura.UmidadeSolo);

        // Obtém ou cria o estado de monitoramento do talhão
        var estadoAtual = await _estadoRepository.ObterAsync(leitura.IdTalhao, ct);

        if (estadoAtual is null)
        {
            _logger.LogDebug("Estado não encontrado para talhão {IdTalhao}, criando novo", leitura.IdTalhao);
            estadoAtual = new EstadoMonitoramentoTalhao
            {
                IdTalhao = leitura.IdTalhao,
                UltimaUmidadeSolo = leitura.UmidadeSolo,
                UltimaLeituraEm = leitura.DataLeitura,
                SecoDesde = null
            };
        }

        // Verifica regra de seca
        await ProcessarRegraSeca(estadoAtual, leitura, ct);

        // Atualiza o estado para a próxima leitura
        estadoAtual.UltimaUmidadeSolo = leitura.UmidadeSolo;
        estadoAtual.UltimaLeituraEm = leitura.DataLeitura;

        await _estadoRepository.UpsertAsync(estadoAtual, ct);
    }

    private async Task ProcessarRegraSeca(EstadoMonitoramentoTalhao estado, LeituraSensorIngerida leitura, CancellationToken ct)
    {
        // Se umidade abaixo do limite de seca
        if (leitura.UmidadeSolo < LIMITE_UMIDADE_SECA)
        {
            _logger.LogInformation("Umidade abaixo do limite: {Umidade}% < {Limite}%", leitura.UmidadeSolo, LIMITE_UMIDADE_SECA);

            // Verifica se já não há alerta ativo
            var alertaExistente = await _alertaRepository.ObterAtivoPorTalhaoETipoAsync(leitura.IdTalhao, TipoAlerta.Seca, ct);

            if (alertaExistente is null)
            {
                _logger.LogInformation("Criando novo alerta de seca para talhão {IdTalhao}", leitura.IdTalhao);

                // Se é a primeira vez, marca o início
                if (!estado.SecoDesde.HasValue)
                {
                    estado.SecoDesde = leitura.DataLeitura;
                }

                // Cria novo alerta
                var novoAlerta = new Alerta
                {
                    Id = string.Empty,
                    IdTalhao = leitura.IdTalhao,
                    Tipo = TipoAlerta.Seca,
                    Mensagem = $"Umidade do solo abaixo de {LIMITE_UMIDADE_SECA}% (Leitura: {leitura.UmidadeSolo}%)",
                    CriadoEm = leitura.DataLeitura
                };

                var alertaId = await _alertaRepository.InserirAsync(novoAlerta, ct);
                _logger.LogInformation("Alerta de seca criado com ID: {AlertaId}", alertaId);
            }
            else
            {
                _logger.LogDebug("Alerta ativo já existe para talhão {IdTalhao}", leitura.IdTalhao);
            }
        }
        else
        {
            _logger.LogDebug("Umidade normal: {Umidade}% >= {Limite}%", leitura.UmidadeSolo, LIMITE_UMIDADE_SECA);

            // Umidade acima do limite: reseta o estado de seca
            if (estado.SecoDesde.HasValue)
            {
                _logger.LogInformation("Resolvendo alerta de seca para talhão {IdTalhao}", leitura.IdTalhao);
                estado.SecoDesde = null;

                // Resolve alertas de seca ativos
                var alertaAtivo = await _alertaRepository.ObterAtivoPorTalhaoETipoAsync(leitura.IdTalhao, TipoAlerta.Seca, ct);
                if (alertaAtivo is not null)
                {
                    await _alertaRepository.ResolverAsync(alertaAtivo.Id, leitura.DataLeitura, ct);
                }
            }
        }
    }
}
