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
		ILoggerFactory loggerFactory,
		RabbitMQConfiguration rabbitMqConfiguration)
	{
		var serviceProvider = services.BuildServiceProvider();
		
		var serviceBus = serviceProvider.GetService<IServiceBus>();
		if (serviceBus != null)
			return services;

		services.AddSingleton(Enumerable.Empty<IConsumer>());
		
		services.AddSingleton(new MufloneConnectionFactory(rabbitMqConfiguration, loggerFactory));
		services.AddSingleton(rabbitMqConfiguration);
		services.AddSingleton<IMufloneConnectionFactory, MufloneConnectionFactory>();
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