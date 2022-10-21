using Microsoft.Extensions.DependencyInjection;
using Muflone.Persistence;
using Muflone.Transport.RabbitMQ.Abstracts;
using Muflone.Transport.RabbitMQ.Factories;
using Muflone.Transport.RabbitMQ.Models;

namespace Muflone.Transport.RabbitMQ;

public static class TransportRabbitMQHelper
{
    public static IServiceCollection AddMufloneTransportRabbitMQ(this IServiceCollection services,
        RabbitMQConfiguration rabbitMQConfiguration,
        RabbitMQReference rabbitMQReference,
        IEnumerable<IConsumer> messageConsumers)
    {
        foreach (var consumer in messageConsumers)
        {
            consumer.StartAsync(CancellationToken.None);
        }

        services.AddSingleton(rabbitMQReference);
        services.AddSingleton(rabbitMQConfiguration);
        services.AddSingleton<IMufloneConnectionFactory, MufloneConnectionFactory>();
        services.AddSingleton<IServiceBus, ServiceBus>();
        services.AddSingleton<IEventBus, ServiceBus>();

        return services;
    }
}