﻿using Microsoft.Extensions.Logging;
using Muflone.Transport.RabbitMQ.Abstracts;
using Muflone.Transport.RabbitMQ.Models;
using Polly;
using RabbitMQ.Client;

namespace Muflone.Transport.RabbitMQ.Factories;

public class RabbitMQConnectionFactory : IRabbitMQConnectionFactory
{
	public IConnection Connection { get; private set; } = default!;

	protected bool IsConnected => Connection is { IsOpen: true };
	public string ExchangeCommandsName { get; }
	public string ExchangeEventsName { get; }
	public string ClientId { get; }

	private readonly RabbitMQConfiguration _rabbitMQConfiguration;
	private readonly ILogger _logger;

	public RabbitMQConnectionFactory(RabbitMQConfiguration rabbitMQConfiguration, ILoggerFactory loggerFactory)
	{
		_logger = loggerFactory.CreateLogger(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));
		_rabbitMQConfiguration = rabbitMQConfiguration ?? throw new ArgumentNullException(nameof(rabbitMQConfiguration));
		ExchangeCommandsName = rabbitMQConfiguration.ExchangeCommandsName;
		ExchangeEventsName = rabbitMQConfiguration.ExchangeEventsName;
		ClientId = rabbitMQConfiguration.ClientId;

		TryCreateConnection();
	}

	public IModel CreateChannel()
	{
		if (!IsConnected)
			throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");

		return Connection.CreateModel();
	}


	private void TryCreateConnection()
	{
		var policy = Policy
			.Handle<Exception>()
			.WaitAndRetry(5,
				retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
				(ex, timeSpan, context) =>
				{
					_logger.LogError(ex, $"an exception has occurred while opening RabbitMQ connection: {ex.Message}");
				});

		Connection = policy.Execute(() => CreateConnection(_rabbitMQConfiguration));
		Connection.ConnectionShutdown += (s, e) => TryCreateConnection();
		Connection.CallbackException += (s, e) => TryCreateConnection();
		Connection.ConnectionBlocked += (s, e) => TryCreateConnection();
	}

	private static IConnection CreateConnection(RabbitMQConfiguration rabbitMQConfiguration)
	{
		var connectionFactory = new ConnectionFactory
		{
			HostName = rabbitMQConfiguration.HostName,
			UserName = rabbitMQConfiguration.UserName,
			Password = rabbitMQConfiguration.Password,
			VirtualHost = rabbitMQConfiguration.VirtualHost,
			Port = AmqpTcpEndpoint.UseDefaultPort,
			DispatchConsumersAsync = true
		};

		return connectionFactory.CreateConnection();
	}
}