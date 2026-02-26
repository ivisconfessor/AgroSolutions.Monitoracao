namespace AgroSolutions.Monitoracao.Dominio;

public enum TipoAlerta
{
    Seca = 1
}

public enum StatusAlerta
{
    Ativo = 1,
    Resolvido = 2
}

/// <summary>
/// Representa um alerta gerado pelo motor de monitoramento.
/// </summary>
public class Alerta
{
    public string Id { get; set; } = null!;
    public Guid IdTalhao { get; set; }
    public TipoAlerta Tipo { get; set; }
    public string Mensagem { get; set; } = null!;
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? ResolvidoEm { get; set; }

    public StatusAlerta Status => ResolvidoEm.HasValue ? StatusAlerta.Resolvido : StatusAlerta.Ativo;
}

/// <summary>
/// Estado de monitoramento mínimo para aplicar regra de seca sem guardar toda a série histórica.
/// </summary>
public class EstadoMonitoramentoTalhao
{
    public Guid IdTalhao { get; set; }
    public DateTimeOffset? SecoDesde { get; set; }
    public DateTimeOffset UltimaLeituraEm { get; set; }
    public double UltimaUmidadeSolo { get; set; }
}
