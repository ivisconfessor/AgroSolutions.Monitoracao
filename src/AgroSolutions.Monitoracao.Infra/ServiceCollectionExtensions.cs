using AgroSolutions.Monitoracao.Dominio;
using AgroSolutions.Monitoracao.Infra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace AgroSolutions.Monitoracao.Infra;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMonitoracaoInfra(this IServiceCollection services, IConfiguration configuration)
    {
        var mongoConnection = configuration["MongoDb:ConnectionString"]
            ?? throw new InvalidOperationException("MongoDb:ConnectionString não está configurada.");
        var databaseName = configuration["MongoDb:DatabaseName"] ?? "agrosolutions_monitoracao";

        var mongoClient = new MongoClient(mongoConnection);
        var database = mongoClient.GetDatabase(databaseName);

        services.AddSingleton<IMongoDatabase>(database);
        services.AddSingleton<IAlertaRepository, AlertaRepository>();
        services.AddSingleton<IEstadoMonitoramentoRepository, EstadoMonitoramentoRepository>();

        return services;
    }
}
