using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Muflone.Persistence;
using Muflone.Transport.RabbitMQ.Abstracts;
using Muflone.Transport.RabbitMQ.Factories;
using Muflone.Transport.RabbitMQ.Models;

namespace Muflone.Transport.RabbitMQ;

public static class TransportRabbitMQHelper
{
	public static IServiceCollection AddMufloneTransportRabbitMQ(this IServiceCollection services,
		RabbitMQConfiguration rabbitMQConfiguration,
		RabbitMQReference rabbitMQReference)
	{
		var serviceProvider = services.BuildServiceProvider();
		var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
		var mufloneConnectionFactory = new MufloneConnectionFactory(rabbitMQConfiguration, loggerFactory!);

		services.AddSingleton(mufloneConnectionFactory);
		services.AddSingleton(rabbitMQReference);
		services.AddSingleton(rabbitMQConfiguration);
		services.AddSingleton<IMufloneConnectionFactory, MufloneConnectionFactory>();
		services.AddSingleton<IServiceBus, ServiceBus>();
		services.AddSingleton<IEventBus, ServiceBus>();

		return services;
	}

	public static IServiceCollection RegisterConsumersInTransportRabbitMQ(this IServiceCollection services,
		IEnumerable<IConsumer> messageConsumers)
	{
		services.AddSingleton(messageConsumers);
		services.AddHostedService<RabbitMqStarter>();

		return services;
	}
}