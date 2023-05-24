﻿using Microsoft.Extensions.Logging;
using Muflone.Messages;
using Muflone.Messages.Events;
using Muflone.Persistence;
using Muflone.Transport.RabbitMQ.Abstracts;
using Muflone.Transport.RabbitMQ.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Muflone.Transport.RabbitMQ.Consumers;

public abstract class IntegrationEventsConsumerBase<T> : ConsumerBase, IIntegrationEventConsumer<T>, IAsyncDisposable
	where T : class, IIntegrationEvent
{
	private readonly ISerializer _messageSerializer;
	private readonly IMufloneConnectionFactory _mufloneConnectionFactory;
	private IModel _channel;
	private readonly RabbitMQReference _rabbitMQReference;

	public string TopicName { get; }

	protected abstract IEnumerable<IIntegrationEventHandlerAsync<T>> HandlersAsync { get; }

	protected IntegrationEventsConsumerBase(IMufloneConnectionFactory mufloneConnectionFactory,
		RabbitMQReference rabbitMQReference, ILoggerFactory loggerFactory) : base(loggerFactory)
	{
		_rabbitMQReference = rabbitMQReference ?? throw new ArgumentNullException(nameof(rabbitMQReference));
		_mufloneConnectionFactory =
			mufloneConnectionFactory ?? throw new ArgumentNullException(nameof(mufloneConnectionFactory));
		_messageSerializer = new Serializer();

		TopicName = typeof(T).Name;
	}

	public async Task ConsumeAsync(T message, CancellationToken cancellationToken = default)
	{
		foreach (var handlerAsync in HandlersAsync)
			await handlerAsync.HandleAsync((dynamic)message, cancellationToken);
	}

	public Task StartAsync(CancellationToken cancellationToken = default)
	{
		InitChannel();
		InitSubscription();

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken = default)
	{
		StopChannel();
		return Task.CompletedTask;
	}

	private void InitChannel()
	{
		StopChannel();

		_channel = _mufloneConnectionFactory.CreateChannel();

		Logger.LogInformation(
			$"initializing retry queue '{TopicName}' on exchange '{_rabbitMQReference.ExchangeEventsName}'...");

		_channel.ExchangeDeclare(_rabbitMQReference.ExchangeEventsName, ExchangeType.Topic);
		_channel.QueueDeclare(TopicName,
			true,
			false,
			false);
		_channel.QueueBind(TopicName,
			_rabbitMQReference.ExchangeEventsName,
			TopicName, //_queueReferences.RoutingKey
			null);

		_channel.CallbackException += OnChannelException;
	}

	private void StopChannel()
	{
		if (_channel is null)
			return;

		_channel.CallbackException -= OnChannelException;

		if (_channel.IsOpen)
			_channel.Close();

		_channel.Dispose();
		_channel = null;
	}

	private void OnChannelException(object _, CallbackExceptionEventArgs ea)
	{
		Logger.LogError(ea.Exception, "the RabbitMQ Channel has encountered an error: {ExceptionMessage}",
			ea.Exception.Message);

		InitChannel();
		InitSubscription();
	}

	private void InitSubscription()
	{
		var consumer = new AsyncEventingBasicConsumer(_channel);

		consumer.Received += OnMessageReceivedAsync;

		Logger.LogInformation($"initializing subscription on queue '{TopicName}' ...");
		_channel.BasicConsume(TopicName, false, consumer);
	}

	private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
	{
		var consumer = sender as IBasicConsumer;
		var channel = consumer?.Model ?? _channel;

		IIntegrationEvent message;
		try
		{
			message = await _messageSerializer.DeserializeAsync<T>(Encoding.ASCII.GetString(eventArgs.Body.ToArray()),
				CancellationToken.None);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex,
				"an exception has occured while decoding queue message from Exchange '{ExchangeName}', message cannot be parsed. Error: {ExceptionMessage}",
				eventArgs.Exchange, ex.Message);
			channel.BasicReject(eventArgs.DeliveryTag, false);
			return;
		}

		Logger.LogInformation(
			"received message '{MessageId}' from Exchange '{ExchangeName}', Queue '{QueueName}'. Processing...",
			message.MessageId, TopicName, TopicName);

		try
		{
			//TODO: provide valid cancellation token
			await ConsumeAsync((dynamic)message, CancellationToken.None);

			channel.BasicAck(eventArgs.DeliveryTag, false);
		}
		catch (Exception ex)
		{
			HandleConsumerException(ex, eventArgs, channel, message, false);
		}
	}

	private void HandleConsumerException(Exception ex, BasicDeliverEventArgs deliveryProps, IModel channel,
		IMessage message, bool requeue)
	{
		var errorMsg =
			"an error has occurred while processing Message '{MessageId}' from Exchange '{ExchangeName}' : {ExceptionMessage} . "
			+ (requeue ? "Reenqueuing..." : "Nacking...");

		Logger.LogWarning(ex, errorMsg, message.MessageId, TopicName, ex.Message);

		if (!requeue)
		{
			channel.BasicReject(deliveryProps.DeliveryTag, false);
		}
		else
		{
			channel.BasicAck(deliveryProps.DeliveryTag, false);
			channel.BasicPublish(
				TopicName,
				deliveryProps.RoutingKey,
				deliveryProps.BasicProperties,
				deliveryProps.Body);
		}
	}

	#region Dispose

	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}

	#endregion
}