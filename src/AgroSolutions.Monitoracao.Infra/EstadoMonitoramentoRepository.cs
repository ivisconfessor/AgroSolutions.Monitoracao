using AgroSolutions.Monitoracao.Dominio;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace AgroSolutions.Monitoracao.Infra;

internal class EstadoMonitoramentoDocument
{
    [BsonId]
    [BsonElement("id_talhao")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public Guid IdTalhao { get; set; }

    [BsonElement("seco_desde")]
    [BsonRepresentation(MongoDB.Bson.BsonType.DateTime)]
    public DateTime? SecoDesde { get; set; }

    [BsonElement("ultima_leitura_em")]
    [BsonRepresentation(MongoDB.Bson.BsonType.DateTime)]
    public DateTime UltimaLeituraEm { get; set; }

    [BsonElement("ultima_umidade_solo")]
    public double UltimaUmidadeSolo { get; set; }
}

public interface IEstadoMonitoramentoRepository
{
    Task<EstadoMonitoramentoTalhao?> ObterAsync(Guid idTalhao, CancellationToken ct = default);
    Task UpsertAsync(EstadoMonitoramentoTalhao estado, CancellationToken ct = default);
}

public class EstadoMonitoramentoRepository : IEstadoMonitoramentoRepository
{
    private readonly IMongoCollection<EstadoMonitoramentoDocument> _collection;

    public EstadoMonitoramentoRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<EstadoMonitoramentoDocument>("monitoramento_talhoes");
    }

    public async Task<EstadoMonitoramentoTalhao?> ObterAsync(Guid idTalhao, CancellationToken ct = default)
    {
        var doc = await _collection.Find(d => d.IdTalhao == idTalhao).FirstOrDefaultAsync(ct);
        return doc is null ? null : ParaDominio(doc);
    }

    public async Task UpsertAsync(EstadoMonitoramentoTalhao estado, CancellationToken ct = default)
    {
        var doc = ParaDocument(estado);
        await _collection.ReplaceOneAsync(
            d => d.IdTalhao == estado.IdTalhao,
            doc,
            new ReplaceOptions { IsUpsert = true },
            ct
        );
    }

    private static EstadoMonitoramentoDocument ParaDocument(EstadoMonitoramentoTalhao estado) => new()
    {
        IdTalhao = estado.IdTalhao,
        SecoDesde = estado.SecoDesde?.UtcDateTime,
        UltimaLeituraEm = estado.UltimaLeituraEm.UtcDateTime,
        UltimaUmidadeSolo = estado.UltimaUmidadeSolo
    };

    private static EstadoMonitoramentoTalhao ParaDominio(EstadoMonitoramentoDocument doc) => new()
    {
        IdTalhao = doc.IdTalhao,
        SecoDesde = doc.SecoDesde is null ? null : new DateTimeOffset(doc.SecoDesde.Value, TimeSpan.Zero),
        UltimaLeituraEm = new DateTimeOffset(doc.UltimaLeituraEm, TimeSpan.Zero),
        UltimaUmidadeSolo = doc.UltimaUmidadeSolo
    };
}
