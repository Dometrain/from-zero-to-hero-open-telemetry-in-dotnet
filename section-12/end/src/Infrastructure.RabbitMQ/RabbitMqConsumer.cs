using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OpenTelemetry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.RabbitMQ;

public class RabbitMqConsumer<T>
{
    private readonly IEventHandler<T> _handler;

    private readonly ConnectionFactory _factory;

    public RabbitMqConsumer(string connectionString, IEventHandler<T> handler)
    {
        _handler = handler;
        _factory = new ConnectionFactory()
        {
            Uri = new Uri(connectionString)
        };
    }

    public void StartConsuming(string exchange, string queueName)
    {
        var connection = _factory.CreateConnection();
        var channel = connection.CreateModel();

        var consumer = new EventingBasicConsumer(channel);

        channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout, durable: true);

        var queueResult = channel.QueueDeclare(queueName, true, false, false, null);
        channel.QueueBind(queue: queueResult.QueueName, exchange: exchange, routingKey: string.Empty);

        consumer.Received += async (_, @event) =>
        {
            var parentContext = RabbitMqDiagnostics.Propagator.Extract(default,
                @event.BasicProperties,
                ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;
            
            // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
            // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md
            const string operation = "process";
            var activityName = $"{@event.RoutingKey} {operation}";

            using var activity = RabbitMqDiagnostics.ActivitySource.StartActivity(activityName, ActivityKind.Consumer,
                parentContext.ActivityContext);
            
            SetActivityContext(activity, @event.RoutingKey, operation);

            var body = @event.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var data = JsonSerializer.Deserialize<T>(message);

            activity?.SetTag("message", message); // DEMO ONLY
            activity?.SetTag("client.id", Baggage.Current.GetBaggage("client.id"));
            
            await _handler.HandleAsync(data!);
        };

        channel.BasicConsume(queue: queueResult.QueueName,
            autoAck: true,
            consumer: consumer);
    }

    private static void SetActivityContext(Activity? activity, string eventName, string operation)
    {
        if (activity is null) return;
        // These tags are added demonstrating the semantic conventions of the OpenTelemetry messaging specification
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md
        activity.SetTag("messaging.system", "rabbitmq");
        activity.SetTag("messaging.destination_kind", "queue");
        activity.SetTag("messaging.operation", operation);
        activity.SetTag("messaging.destination.name", eventName);
    }
    private IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
    {
        if (!props.Headers.TryGetValue(key, out var value)) return [];

        var bytes = value as byte[];
        return [Encoding.UTF8.GetString(bytes)];
    }
}