using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Infrastructure.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Reporting.Diagnostics;

namespace Reporting;

public class ReportingHostedService : BackgroundService
{
    private readonly ILogger<ReportingHostedService> _logger;
    private readonly int _batchSize;
    private readonly string _queueName;
    private readonly ConcurrentBag<string> _messageBatch = new();
    private readonly ConcurrentBag<ActivityLink> _activityLinks = new();
    private readonly object _batchLock = new();
    private readonly IModel _channel;
    private readonly IConnection _connection;

    public ReportingHostedService(ILogger<ReportingHostedService> logger, string connectionString,
        string exchange, string queueName, int batchSize)
    {
        _logger = logger;
        _queueName = queueName;
        _batchSize = batchSize;

        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout, durable: true);

        var queueResult = _channel.QueueDeclare(_queueName, true, false, false, null);
        _channel.QueueBind(queue: queueResult.QueueName, exchange: exchange, routingKey: string.Empty);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hosted Service running.");

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            var parentContext = RabbitMqDiagnostics.Propagator
                .Extract(default, ea.BasicProperties,
                ExtractTraceContextFromBasicProperties);
            
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation("New message...");

            lock (_batchLock)
            {
                _messageBatch.Add(message);
                _activityLinks.Add(
                    new ActivityLink(parentContext.ActivityContext));

                if (_messageBatch.Count < _batchSize) return;

                ProcessBatch(_messageBatch, _activityLinks);
                _messageBatch.Clear();
                _activityLinks.Clear();
            }
        };

        _channel.BasicConsume(queue: _queueName,
            autoAck: true,
            consumer: consumer);

        await BackgroundProcessing(stoppingToken);
    }

    private static IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
    {
        if (!props.Headers.TryGetValue(key, out var value)) return [];

        var bytes = value as byte[];
        return [Encoding.UTF8.GetString(bytes)];
    }



    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Hosted Service is working...");

            lock (_batchLock)
            {
                if (_messageBatch.Count <= 0) return;

                ProcessBatch(_messageBatch, _activityLinks);

                _messageBatch.Clear();
                _activityLinks.Clear();
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hosted Service is stopping.");

        await base.StopAsync(stoppingToken);
    }

    public override void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
        base.Dispose();
    }

    private static void ProcessBatch(ConcurrentBag<string> messageBatch,
        ConcurrentBag<ActivityLink> links)
    {
        using var activity = ApplicationDiagnostics.ActivitySource
            .StartActivity("Report Process",
                ActivityKind.Internal,
                new ActivityContext(),
                links: links);
        
        Console.WriteLine($"Processing batch of {messageBatch.Count} messages.");
        foreach (var message in messageBatch)
        {
            Console.WriteLine(message);
        }
    }
}