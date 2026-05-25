using Microsoft.Extensions.Logging;
using Muflone.Messages;
using Muflone.Transport.RabbitMQ.Abstracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Security.Cryptography;
using System.Text;

namespace Muflone.Transport.RabbitMQ.Consumers;

public class RabbitMQSubscriber(
		ILoggerFactory loggerFactory,
		IServiceProvider serviceProvider,
		IRabbitMQConnectionFactory connectionFactory) : MessageSubscriberBase<IChannel>(loggerFactory, serviceProvider)
{
	private readonly ILogger _logger = loggerFactory.CreateLogger<RabbitMQSubscriber>();

	protected override async Task StopChannelAsync(HandlerSubscription<IChannel> handlerSubscription)
	{
		if (handlerSubscription.Channel == null || handlerSubscription.Channel.IsClosed) return;

		//handlerSubscription.Channel.CallbackException -= OnChannelExceptionAsync;

		await handlerSubscription.Channel.CloseAsync();
		handlerSubscription.Channel.Dispose();
		handlerSubscription.Channel = null;
	}

	private async Task InitExchangesAsync()
	{
		await using var channel = await connectionFactory.CreateChannelAsync();
		await channel.ExchangeDeclareAsync(connectionFactory.ExchangeEventsName, ExchangeType.Topic, durable: true);
		await channel.ExchangeDeclareAsync(connectionFactory.ExchangeCommandsName, ExchangeType.Direct, durable: true);
	}

	protected override async Task InitChannelAsync(HandlerSubscription<IChannel> handlerSubscription)
	{
		await InitExchangesAsync();

		var channel = await connectionFactory.CreateChannelAsync();

		var queueName = GetQueueName(handlerSubscription);
		var routingKey = GetRoutingKey(handlerSubscription);

		var exchangeName = handlerSubscription.IsCommandHandler
				? connectionFactory.ExchangeCommandsName
				: connectionFactory.ExchangeEventsName;

		await channel.QueueDeclareAsync(queueName, true, false, false);

		await channel.QueueBindAsync(queueName, exchangeName, routingKey, null);

		channel.CallbackExceptionAsync += async (_, e) =>
		{
			_logger.LogWarning($"Channel exception: {e.Exception.Message}");
			await OnChannelExceptionAsync(handlerSubscription, e);
		};

		handlerSubscription.Channel = channel;
	}

	private async Task OnChannelExceptionAsync(HandlerSubscription<IChannel> handlerSubscription,
			CallbackExceptionEventArgs e)
	{
		await InitChannelAsync(handlerSubscription);
		await InitSubscriptionAsync(handlerSubscription);
	}

	protected override async Task InitSubscriptionAsync(HandlerSubscription<IChannel> handlerSubscription)
	{
		var queueName = GetQueueName(handlerSubscription);
		var consumer = new AsyncEventingBasicConsumer(handlerSubscription.Channel!);
		consumer.ReceivedAsync += async (_, @event) =>
		{
			var messageString = Encoding.UTF8.GetString(@event.Body.ToArray());
			await handlerSubscription.MessageAsync(messageString, CancellationToken.None);
			await handlerSubscription.Channel!.BasicAckAsync(@event.DeliveryTag, false);
		};

		await handlerSubscription.Channel!.BasicConsumeAsync(queueName, false, consumer);
	}

	private string GetQueueName(HandlerSubscription<IChannel> handlerSubscription)
	{
		if (handlerSubscription.Configuration?.QueueName is not null)
			return handlerSubscription.Configuration.QueueName;

		var baseQueueName = $"{connectionFactory.ClientId}.{handlerSubscription.EventTypeName}";

		// For events, always use unique queues per handler type; for commands, use singleton logic.
		// ConsumerTypeName is the fully-qualified type name, guaranteeing uniqueness across namespaces.
		if (!handlerSubscription.IsCommandHandler || !handlerSubscription.IsSingletonHandler)
		{
			var consumerTypeName = handlerSubscription.ConsumerTypeName;
			var candidate = $"{baseQueueName}.{consumerTypeName}";

			const int maxQueueNameLength = 255;
			if (candidate.Length <= maxQueueNameLength)
				return candidate;

			// Full name exceeds the limit: hash the consumer type name to a stable 16-char hex suffix.
			var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(consumerTypeName));
			var hash = Convert.ToHexString(hashBytes)[..16];
			var hashCandidate = $"{baseQueueName}.{hash}";
			return hashCandidate[..Math.Min(hashCandidate.Length, maxQueueNameLength)];
		}

		const int maxLength = 255;
		return baseQueueName[..Math.Min(baseQueueName.Length, maxLength)];
	}

	private static string GetRoutingKey(HandlerSubscription<IChannel> handlerSubscription)
	{
		return handlerSubscription.Configuration?.RoutingKey ?? handlerSubscription.EventTypeName;
	}
}