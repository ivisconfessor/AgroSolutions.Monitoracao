using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace AgroSolutions.Monitoracao.Infra;

/// <summary>
/// Mensagem recebida da fila do RabbitMQ (publicada pelo AgroSolutions.Sensores).
/// </summary>
public record LeituraSensorIngerida(
    string Id,
    Guid IdTalhao,
    DateTimeOffset DataLeitura,
    double UmidadeSolo,
    double Temperatura,
    double Precipitacao
);

public interface ILeiturasQueueConsumer
{
    Task StartAsync(Func<LeituraSensorIngerida, Task> onMessage, CancellationToken ct);
    Task StopAsync();
}

/// <summary>
/// Consumer RabbitMQ simples (fila agrosolutions.sensores.leituras) com ack manual.
/// </summary>
public class RabbitMqLeiturasQueueConsumer : ILeiturasQueueConsumer, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queueName;
    private readonly AsyncEventingBasicConsumer _consumer;
    private Func<LeituraSensorIngerida, Task>? _onMessage;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RabbitMqLeiturasQueueConsumer(string hostName, string? userName, string? password, string queueName = "agrosolutions.sensores.leituras")
    {
        _queueName = queueName;
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = string.IsNullOrWhiteSpace(userName) ? "guest" : userName,
            Password = string.IsNullOrWhiteSpace(password) ? "guest" : password,
            Port = 5672,
            DispatchConsumersAsync = true
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.BasicQos(0, prefetchCount: 20, global: false);

        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var msg = JsonSerializer.Deserialize<LeituraSensorIngerida>(json, JsonOptions);
                if (msg is null)
                {
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                if (_onMessage is not null)
                    await _onMessage(msg);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch
            {
                // Falhou o processamento: reencaminha para reprocessar depois
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };
    }

    public Task StartAsync(Func<LeituraSensorIngerida, Task> onMessage, CancellationToken ct)
    {
        _onMessage = onMessage;
        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: _consumer);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        try { _channel.Close(); } catch { }
        try { _connection.Close(); } catch { }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try { _channel?.Dispose(); } catch { }
        try { _connection?.Dispose(); } catch { }
        GC.SuppressFinalize(this);
    }
}
