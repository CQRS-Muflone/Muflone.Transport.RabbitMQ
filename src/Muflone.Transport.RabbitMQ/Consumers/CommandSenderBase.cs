namespace Muflone.Transport.RabbitMQ.Consumers;

public abstract class CommandSenderBase<T> : ConsumerBase, ICommandSender<T>, IAsyncDisposable
	where T : Command
{
	private readonly ConsumerConfiguration _configuration;
	private readonly IMufloneConnectionFactory _connectionFactory;
	private IModel _channel;

	protected CommandSenderBase(IMufloneConnectionFactory connectionFactory,
		ILoggerFactory loggerFactory)
		: this(new ConsumerConfiguration(), connectionFactory, loggerFactory)
	{
	}

	protected CommandSenderBase(ConsumerConfiguration configuration, IMufloneConnectionFactory connectionFactory,
		ILoggerFactory loggerFactory) : base(loggerFactory)
	{
		Repository = repository ?? throw new ArgumentNullException(nameof(repository));
		_connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

		if (string.IsNullOrWhiteSpace(configuration.ResourceKey))
			configuration.ResourceKey = typeof(T).Name;

		if (string.IsNullOrWhiteSpace(configuration.QueueName))
		{
			configuration.QueueName = GetType().Name;
			if (configuration.QueueName.EndsWith("Consumer", StringComparison.InvariantCultureIgnoreCase))
				configuration.QueueName =
					configuration.QueueName.Substring(0, configuration.QueueName.Length - "Consumer".Length);
		}

		_configuration = configuration;
	}

	public Task StartAsync(CancellationToken cancellationToken = default)
	{
		InitChannel();

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

		_channel = _connectionFactory.CreateChannel();

		Logger.LogInformation(
			$"initializing retry queue '{_configuration.QueueName}' on exchange '{_connectionFactory.ExchangeCommandsName}'...");

		_channel.ExchangeDeclare(_connectionFactory.ExchangeCommandsName, ExchangeType.Direct);
		_channel.QueueDeclare(_configuration.QueueName,
			true,
			false,
			false);
		_channel.QueueBind(_configuration.QueueName,
			_connectionFactory.ExchangeCommandsName,
			_configuration.ResourceKey,
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
		Logger.LogError(ea.Exception, $"RabbitMQ Channel has encountered an error: {ea.Exception.Message}");

		InitChannel();
	}

	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}
}