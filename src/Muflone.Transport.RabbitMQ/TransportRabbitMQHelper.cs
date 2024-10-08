﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Muflone.Persistence;
using Muflone.Transport.RabbitMQ.Abstracts;
using Muflone.Transport.RabbitMQ.Factories;
using Muflone.Transport.RabbitMQ.Models;

namespace Muflone.Transport.RabbitMQ;

public static class TransportRabbitMQHelper
{
	public static IServiceCollection AddMufloneTransportRabbitMQ(this IServiceCollection services,
		ILoggerFactory loggerFactory,
		RabbitMQConfiguration rabbitMQConfiguration)
	{
		services.AddSingleton(new RabbitMQConnectionFactory(rabbitMQConfiguration, loggerFactory));
		services.AddSingleton(rabbitMQConfiguration);
		services.AddSingleton<IRabbitMQConnectionFactory, RabbitMQConnectionFactory>();
		services.AddSingleton<IServiceBus, ServiceBus>();
		services.AddSingleton<IEventBus, ServiceBus>();
		services.AddHostedService<RabbitMqStarter>();

		return services;
	}

	public static IServiceCollection AddMufloneRabbitMQConsumers(this IServiceCollection services,
		IEnumerable<IConsumer> consumers)
	{
		services.AddSingleton(consumers);
		return services;
	}
}