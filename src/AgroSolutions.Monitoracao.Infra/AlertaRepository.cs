using AgroSolutions.Monitoracao.Dominio;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace AgroSolutions.Monitoracao.Infra;

internal class AlertaDocument
{
    [BsonId]
    public string Id { get; set; } = null!;

    [BsonElement("id_talhao")]
    public Guid IdTalhao { get; set; }

    [BsonElement("tipo")]
    public string Tipo { get; set; } = null!;

    [BsonElement("mensagem")]
    public string Mensagem { get; set; } = null!;

    [BsonElement("criado_em")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime CriadoEm { get; set; }

    [BsonElement("resolvido_em")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime? ResolvidoEm { get; set; }
}

public interface IAlertaRepository
{
    Task<string> InserirAsync(Alerta alerta, CancellationToken ct = default);
    Task<Alerta?> ObterPorIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Alerta>> ListarPorTalhaoAsync(Guid idTalhao, bool somenteAtivos, int limite, CancellationToken ct = default);
    Task<Alerta?> ObterAtivoPorTalhaoETipoAsync(Guid idTalhao, TipoAlerta tipo, CancellationToken ct = default);
    Task ResolverAsync(string id, DateTimeOffset resolvidoEm, CancellationToken ct = default);
}

public class AlertaRepository : IAlertaRepository
{
    private readonly IMongoCollection<AlertaDocument> _collection;

    public AlertaRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<AlertaDocument>("alertas");
    }

    public async Task<string> InserirAsync(Alerta alerta, CancellationToken ct = default)
    {
        var doc = ParaDocument(alerta);
        doc.Id = Guid.NewGuid().ToString();
        await _collection.InsertOneAsync(doc, cancellationToken: ct);
        return doc.Id;
    }

    public async Task<Alerta?> ObterPorIdAsync(string id, CancellationToken ct = default)
    {
        var doc = await _collection.Find(d => d.Id == id).FirstOrDefaultAsync(ct);
        return doc is null ? null : ParaDominio(doc);
    }

    public async Task<IReadOnlyList<Alerta>> ListarPorTalhaoAsync(Guid idTalhao, bool somenteAtivos, int limite, CancellationToken ct = default)
    {
        var filter = Builders<AlertaDocument>.Filter.Eq(d => d.IdTalhao, idTalhao);
        if (somenteAtivos)
            filter &= Builders<AlertaDocument>.Filter.Eq(d => d.ResolvidoEm, null);

        var docs = await _collection
            .Find(filter)
            .SortByDescending(d => d.CriadoEm)
            .Limit(limite)
            .ToListAsync(ct);
        return docs.Select(ParaDominio).ToList();
    }

    public async Task<Alerta?> ObterAtivoPorTalhaoETipoAsync(Guid idTalhao, TipoAlerta tipo, CancellationToken ct = default)
    {
        var tipoString = TipoParaString(tipo);
        var doc = await _collection.Find(d => d.IdTalhao == idTalhao && d.Tipo == tipoString && d.ResolvidoEm == null)
            .FirstOrDefaultAsync(ct);
        return doc is null ? null : ParaDominio(doc);
    }

    public async Task ResolverAsync(string id, DateTimeOffset resolvidoEm, CancellationToken ct = default)
    {
        var update = Builders<AlertaDocument>.Update.Set(d => d.ResolvidoEm, resolvidoEm.UtcDateTime);
        await _collection.UpdateOneAsync(d => d.Id == id, update, cancellationToken: ct);
    }

    private static AlertaDocument ParaDocument(Alerta alerta) => new()
    {
        Id = alerta.Id,
        IdTalhao = alerta.IdTalhao,
        Tipo = TipoParaString(alerta.Tipo),
        Mensagem = alerta.Mensagem,
        CriadoEm = alerta.CriadoEm.UtcDateTime,
        ResolvidoEm = alerta.ResolvidoEm?.UtcDateTime
    };

    private static Alerta ParaDominio(AlertaDocument doc) => new()
    {
        Id = doc.Id,
        IdTalhao = doc.IdTalhao,
        Tipo = StringParaTipo(doc.Tipo),
        Mensagem = doc.Mensagem,
        CriadoEm = new DateTimeOffset(doc.CriadoEm, TimeSpan.Zero),
        ResolvidoEm = doc.ResolvidoEm is null ? null : new DateTimeOffset(doc.ResolvidoEm.Value, TimeSpan.Zero)
    };

    private static string TipoParaString(TipoAlerta tipo) => tipo switch
    {
        TipoAlerta.Seca => "seca",
        _ => tipo.ToString().ToLowerInvariant()
    };

    private static TipoAlerta StringParaTipo(string tipo) => tipo.ToLowerInvariant() switch
    {
        "seca" => TipoAlerta.Seca,
        _ => TipoAlerta.Seca
    };
}
