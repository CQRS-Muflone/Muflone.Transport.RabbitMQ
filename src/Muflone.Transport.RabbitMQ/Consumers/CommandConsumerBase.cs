using System.Text;
using Microsoft.Extensions.Logging;
using Muflone.Messages;
using Muflone.Messages.Commands;
using Muflone.Persistence;
using Muflone.Transport.RabbitMQ.Abstracts;
using Muflone.Transport.RabbitMQ.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Muflone.Transport.RabbitMQ.Consumers;

public abstract class CommandConsumerBase<T> : ConsumerBase, ICommandConsumer<T>, IAsyncDisposable where T : class, ICommand
{
    private readonly ISerializer _messageSerializer;
    private readonly IMufloneConnectionFactory _mufloneConnectionFactory;
    private IModel _channel;
    private readonly RabbitMQReference _rabbitMQReference;

    protected abstract ICommandHandlerAsync<T> HandlerAsync { get; }

    public string TopicName { get; }

    protected CommandConsumerBase(RabbitMQReference rabbitMQReference,
        IMufloneConnectionFactory mufloneConnectionFactory,
        ILoggerFactory loggerFactory) : base(loggerFactory)
    {
        _rabbitMQReference = rabbitMQReference ?? throw new ArgumentNullException(nameof(rabbitMQReference));
        _mufloneConnectionFactory = mufloneConnectionFactory ?? throw new ArgumentNullException(nameof(mufloneConnectionFactory));
        _messageSerializer = new Serializer();

        TopicName = typeof(T).Name;
    }

    public async Task ConsumeAsync(T message, CancellationToken cancellationToken = default)
    {
        await HandlerAsync.HandleAsync(message, cancellationToken);
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

        Logger.LogInformation($"initializing retry queue '{TopicName}' on exchange '{_rabbitMQReference.ExchangeCommandsName}'...");

        _channel.ExchangeDeclare(exchange: _rabbitMQReference.ExchangeCommandsName, type: ExchangeType.Direct);
        _channel.QueueDeclare(queue: TopicName,
            durable: true,
            exclusive: false,
            autoDelete: false);
        _channel.QueueBind(queue: _rabbitMQReference.QueueCommandsName,
            exchange: _rabbitMQReference.ExchangeCommandsName,
            routingKey: TopicName, //_queueReferences.RoutingKey
            arguments: null);

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
        Logger.LogError(ea.Exception, "the RabbitMQ Channel has encountered an error: {ExceptionMessage}", ea.Exception.Message);

        InitChannel();
        InitSubscription();
    }

    private void InitSubscription()
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += OnMessageReceivedAsync;

        Logger.LogInformation($"initializing subscription on queue '{TopicName}' ...");
        _channel.BasicConsume(queue: TopicName, autoAck: false, consumer: consumer);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        var consumer = sender as IBasicConsumer;
        var channel = consumer?.Model ?? _channel;

        ICommand command;
        try
        {
            command = await _messageSerializer.DeserializeAsync<T>(Encoding.ASCII.GetString(eventArgs.Body.ToArray()),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "an exception has occured while decoding queue message from Exchange '{ExchangeName}', message cannot be parsed. Error: {ExceptionMessage}",
                eventArgs.Exchange, ex.Message);
            channel.BasicReject(eventArgs.DeliveryTag, requeue: false);
            return;
        }

        Logger.LogInformation("received message '{MessageId}' from Exchange '{ExchangeName}', Queue '{QueueName}'. Processing...",
            command.MessageId, TopicName, TopicName);

        try
        {
            //TODO: provide valid cancellation token
            await ConsumeAsync((dynamic)command, CancellationToken.None);

            channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            HandleConsumerException(ex, eventArgs, channel, command, false);
        }
    }

    private void HandleConsumerException(Exception ex, BasicDeliverEventArgs deliveryProps, IModel channel, IMessage message, bool requeue)
    {
        var errorMsg = "an error has occurred while processing Message '{MessageId}' from Exchange '{ExchangeName}' : {ExceptionMessage} . "
                       + (requeue ? "Reenqueuing..." : "Nacking...");

        Logger.LogWarning(ex, errorMsg, message.MessageId, TopicName, ex.Message);

        if (!requeue)
            channel.BasicReject(deliveryProps.DeliveryTag, requeue: false);
        else
        {
            channel.BasicAck(deliveryProps.DeliveryTag, false);
            channel.BasicPublish(
                exchange: TopicName,
                routingKey: deliveryProps.RoutingKey,
                basicProperties: deliveryProps.BasicProperties,
                body: deliveryProps.Body);
        }
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}