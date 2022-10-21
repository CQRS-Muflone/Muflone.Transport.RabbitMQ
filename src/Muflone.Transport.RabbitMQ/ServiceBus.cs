using System.Text;
using Microsoft.Extensions.Logging;
using Muflone.Messages.Commands;
using Muflone.Messages.Events;
using Muflone.Persistence;
using Muflone.Transport.RabbitMQ.Abstracts;
using Muflone.Transport.RabbitMQ.Models;
using Polly;
using RabbitMQ.Client;

namespace Muflone.Transport.RabbitMQ;

public class ServiceBus : IServiceBus, IEventBus
{
    private readonly IMufloneConnectionFactory _mufloneConnectionFactory;
    private readonly RabbitMQReference _rabbitMQReference;
    private readonly ISerializer _messageSerializer;
    private readonly ILogger _logger;

    public ServiceBus(IMufloneConnectionFactory mufloneConnectionFactory,
        RabbitMQReference rabbitMQReference,
        ILoggerFactory loggerFactory,
        ISerializer? messageSerializer = null)
    {
        _mufloneConnectionFactory =
            mufloneConnectionFactory ?? throw new ArgumentNullException(nameof(mufloneConnectionFactory));
        _rabbitMQReference = rabbitMQReference ?? throw new ArgumentNullException(nameof(rabbitMQReference));
        _messageSerializer = messageSerializer ?? new Serializer();
        _logger = loggerFactory.CreateLogger(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task SendAsync<T>(T command, CancellationToken cancellationToken = new()) where T : class, ICommand
    {
        var serializedMessage = await _messageSerializer.SerializeAsync(command, cancellationToken);

        var policy = Policy.Handle<Exception>()
            .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogWarning(ex,
                    "Could not publish message '{MessageId}' to Exchange '{ExchangeName}', after {Timeout}s : {ExceptionMessage}",
                    command.MessageId,
                    _rabbitMQReference.ExchangeCommandsName,
                    $"{time.TotalSeconds:n1}", ex.Message);
            });

        var channel = _mufloneConnectionFactory.CreateChannel();
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Headers = new Dictionary<string, object>()
        {
            { "message-type", command.GetType().FullName }
        };

        policy.Execute(() =>
        {
            channel.ExchangeDeclare(exchange: _rabbitMQReference.ExchangeCommandsName, type: ExchangeType.Direct);
            channel.BasicPublish(
                exchange: _rabbitMQReference.ExchangeCommandsName,
                routingKey: _rabbitMQReference.QueueCommandsName,
                mandatory: true,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(serializedMessage));

            _logger.LogInformation("message '{MessageId}' published to Exchange '{ExchangeName}'",
                command.MessageId,
                _rabbitMQReference.ExchangeCommandsName);
        });
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = new()) where T : class, IEvent
    {
        var serializedMessage = await _messageSerializer.SerializeAsync(@event, cancellationToken);

        var policy = Policy.Handle<Exception>()
            .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogWarning(ex,
                    "Could not publish message '{MessageId}' to Exchange '{ExchangeName}', after {Timeout}s : {ExceptionMessage}",
                    @event.MessageId,
                    _rabbitMQReference.ExchangeCommandsName,
                    $"{time.TotalSeconds:n1}", ex.Message);
            });

        var channel = _mufloneConnectionFactory.CreateChannel();
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Headers = new Dictionary<string, object>()
        {
            { "message-type", @event.GetType().FullName }
        };

        policy.Execute(() =>
        {
            channel.ExchangeDeclare(exchange: _rabbitMQReference.ExchangeEventsName, type: ExchangeType.Fanout);
            channel.BasicPublish(
                exchange: _rabbitMQReference.ExchangeEventsName,
                routingKey: _rabbitMQReference.QueueEventsName,
                mandatory: true,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(serializedMessage));

            _logger.LogInformation("message '{MessageId}' published to Exchange '{ExchangeName}'",
                @event.MessageId,
                _rabbitMQReference.ExchangeCommandsName);
        });
    }
}