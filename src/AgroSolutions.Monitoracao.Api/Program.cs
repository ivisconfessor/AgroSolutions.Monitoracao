using AgroSolutions.Monitoracao.Aplicacao;
using AgroSolutions.Monitoracao.Infra;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AgroSolutions.Monitoracao.Api",
        Version = "v1",
        Description = "[HACKATON FIAP] Microserviço motor de alertas - Processa leituras de sensores e gera alertas (ex.: alerta de seca)."
    });
});

builder.Services.AddMonitoracaoInfra(builder.Configuration);
builder.Services.AddSingleton<IMotorAlertas, MotorAlertas>();
builder.Services.AddSingleton<ILeiturasQueueConsumer>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var hostName = config["RabbitMQ:HostName"] ?? "localhost";
    var userName = config["RabbitMQ:UserName"];
    var password = config["RabbitMQ:Password"];
    var queueName = config["RabbitMQ:FilaLeituras"] ?? "agrosolutions.sensores.leituras";
    return new RabbitMqLeiturasQueueConsumer(hostName, userName, password, queueName);
});
builder.Services.AddHostedService<LeiturasQueueConsumerHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "[HACKATON FIAP] - AgroSolutions.Monitoracao.Api v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Endpoints de alertas

app.MapGet("/alertas/{id}", async (string id, IAlertaRepository repo, CancellationToken ct) =>
{
    var alerta = await repo.ObterPorIdAsync(id, ct);
    if (alerta is null)
        return Results.NotFound();
    return Results.Ok(new AlertaResponse(alerta.Id, alerta.IdTalhao, alerta.Tipo.ToString(), alerta.Mensagem, alerta.CriadoEm, alerta.ResolvidoEm, alerta.Status.ToString()));
})
.WithName("ObterAlertaPorId")
.WithOpenApi();

app.MapGet("/alertas", async (Guid? idTalhao, bool somenteAtivos, int? limite, IAlertaRepository repo, CancellationToken ct) =>
{
    if (!idTalhao.HasValue || idTalhao.Value == Guid.Empty)
        return Results.BadRequest(new { mensagem = "Informe idTalhao para listar alertas." });

    var alertas = await repo.ListarPorTalhaoAsync(idTalhao.Value, somenteAtivos, limite ?? 100, ct);
    var response = alertas.Select(a => new AlertaResponse(a.Id, a.IdTalhao, a.Tipo.ToString(), a.Mensagem, a.CriadoEm, a.ResolvidoEm, a.Status.ToString()));
    return Results.Ok(response);
})
.WithName("ListarAlertasPorTalhao")
.WithOpenApi();

app.MapPost("/alertas/{id}/resolver", async (string id, IAlertaRepository repo, CancellationToken ct) =>
{
    var alerta = await repo.ObterPorIdAsync(id, ct);
    if (alerta is null)
        return Results.NotFound();

    await repo.ResolverAsync(id, DateTimeOffset.UtcNow, ct);
    
    var alertaAtualizado = await repo.ObterPorIdAsync(id, ct);
    return Results.Ok(new AlertaResponse(alertaAtualizado!.Id, alertaAtualizado.IdTalhao, alertaAtualizado.Tipo.ToString(), alertaAtualizado.Mensagem, alertaAtualizado.CriadoEm, alertaAtualizado.ResolvidoEm, alertaAtualizado.Status.ToString()));
})
.WithName("ResolverAlerta")
.WithOpenApi();

app.Run();

record AlertaResponse(string Id, Guid IdTalhao, string Tipo, string Mensagem, DateTimeOffset CriadoEm, DateTimeOffset? ResolvidoEm, string Status);
