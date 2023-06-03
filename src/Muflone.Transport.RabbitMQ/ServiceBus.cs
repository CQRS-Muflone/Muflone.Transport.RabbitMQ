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
	private readonly IMufloneConnectionFactory _connectionFactory;
	private readonly ISerializer _messageSerializer;
	private readonly ILogger _logger;

	public ServiceBus(IMufloneConnectionFactory mufloneConnectionFactory, ILoggerFactory loggerFactory, ISerializer? messageSerializer = null)
	{
		_connectionFactory = mufloneConnectionFactory ?? throw new ArgumentNullException(nameof(mufloneConnectionFactory));
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
					_connectionFactory.ExchangeCommandsName,
					$"{time.TotalSeconds:n1}", ex.Message);
			});

		var channel = _connectionFactory.CreateChannel();
		var properties = channel.CreateBasicProperties();
		properties.Persistent = true;
		properties.Headers = new Dictionary<string, object>()
		{
			{ "message-type", command.GetType().FullName! }
		};

		policy.Execute(() =>
		{
			channel.ExchangeDeclare(_connectionFactory.ExchangeCommandsName, ExchangeType.Direct);
			channel.BasicPublish(
				_connectionFactory.ExchangeCommandsName,
				command.GetType().Name,
				true,
				properties,
				Encoding.UTF8.GetBytes(serializedMessage));

			_logger.LogInformation($"message '{command.MessageId}' published to Exchange '{_connectionFactory.ExchangeCommandsName}'",
				command.MessageId,
				_connectionFactory.ExchangeCommandsName);
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
					_connectionFactory.ExchangeCommandsName,
					$"{time.TotalSeconds:n1}", ex.Message);
			});

		var channel = _connectionFactory.CreateChannel();
		var properties = channel.CreateBasicProperties();
		properties.Persistent = true;
		properties.Headers = new Dictionary<string, object>()
		{
			{ "message-type", @event.GetType().FullName! }
		};

		policy.Execute(() =>
		{
			channel.ExchangeDeclare(_connectionFactory.ExchangeEventsName, ExchangeType.Topic);
			channel.BasicPublish(
				_connectionFactory.ExchangeEventsName,
				@event.GetType().Name,
				true,
				properties,
				Encoding.UTF8.GetBytes(serializedMessage));

			_logger.LogInformation($"message '{@event.MessageId}' published to Exchange '{_connectionFactory.ExchangeEventsName}'");
		});
	}
}