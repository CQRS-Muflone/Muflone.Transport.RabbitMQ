using Microsoft.Extensions.Logging;
using Muflone.Messages.Commands;
using Muflone.Messages.Events;
using Muflone.Persistence;
using Muflone.Transport.RabbitMQ.Abstracts;
using Polly;
using RabbitMQ.Client;
using System.Text;

namespace Muflone.Transport.RabbitMQ;

public class ServiceBus : IServiceBus, IEventBus
{
	private readonly IRabbitMQConnectionFactory _connectionFactory;
    private readonly ISerializer _messageSerializer;
    private readonly ILogger _logger;
    private Task<IChannel>? _writerChannelTask;

    public ServiceBus(IRabbitMQConnectionFactory mufloneConnectionFactory, ILoggerFactory loggerFactory,
        ISerializer? messageSerializer = null)
    {
        _connectionFactory = mufloneConnectionFactory
                             ?? throw new ArgumentNullException(nameof(mufloneConnectionFactory));
        _messageSerializer = messageSerializer
                             ?? new Serializer();
        _logger = loggerFactory.CreateLogger(GetType())
                  ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task SendAsync<T>(T request, CancellationToken cancellationToken = default)
        where T : class, ICommand
    {
        var serializedMessage = await _messageSerializer.SerializeAsync(request, cancellationToken);

        var policy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogWarning(ex,
                    "Could not send message '{MessageId}' to Exchange '{ExchangeName}', after {Timeout}s : {ExceptionMessage}",
                    request,
                    _connectionFactory.ExchangeCommandsName,
                    $"{time.TotalSeconds:n1}", ex.Message);
            });

        var properties = new BasicProperties
        {
            Persistent = true,
            Headers = new Dictionary<string, object?>
            {
                { "message-type", request.GetType().FullName! }
            }
        };

        await policy.ExecuteAsync(async () =>
        {
            var channel = await GetWriterChannelAsync();
            await channel.BasicPublishAsync(
                _connectionFactory.ExchangeCommandsName,
                request.GetType().Name,
                true,
                properties,
                Encoding.UTF8.GetBytes(serializedMessage),
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                $"message '{request}' sent to Exchange '{_connectionFactory.ExchangeCommandsName}'",
                request,
                _connectionFactory.ExchangeCommandsName);
        });
    }
    
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class, IEvent
    {
        var serializedMessage =
            await _messageSerializer.SerializeAsync(message, cancellationToken).ConfigureAwait(false);

        var policy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogWarning(ex,
                    "Could not publish message '{MessageId}' to Exchange '{ExchangeName}', after {Timeout}s : {ExceptionMessage}",
                    message,
                    _connectionFactory.ExchangeEventsName,
                    $"{time.TotalSeconds:n1}", ex.Message);
            });

        var properties = new BasicProperties
        {
            Persistent = true,
            Headers = new Dictionary<string, object?>
            {
                { "message-type", message.GetType().FullName! }
            }
        };

        await policy.ExecuteAsync(async () =>
            {
                var channel = await GetWriterChannelAsync().ConfigureAwait(false);
                await channel.BasicPublishAsync(
                        _connectionFactory.ExchangeEventsName,
                        message.GetType().Name,
                        true,
                        properties,
                        Encoding.UTF8.GetBytes(serializedMessage),
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                ;

                _logger.LogInformation(
                    $"message '{message}' published to Exchange '{_connectionFactory.ExchangeEventsName}'");
            })
            .ConfigureAwait(false);
        ;
    }

    private Task<IChannel> GetWriterChannelAsync()
    {
        if (_writerChannelTask == null)
        {
            _writerChannelTask = _connectionFactory.CreateChannelAsync();
        }

        return _writerChannelTask;
    }

    #region Dispose

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            // _poller!.Stop();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ServiceBus()
    {
        Dispose(false);
    }

    #endregion
}