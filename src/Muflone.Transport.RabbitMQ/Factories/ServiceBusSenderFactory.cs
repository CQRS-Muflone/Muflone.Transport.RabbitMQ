using Muflone.Transport.RabbitMQ.Abstracts;
using RabbitMQ.Client;

namespace Muflone.Transport.RabbitMQ.Factories;

public sealed class ServiceBusSenderFactory(IConnection connection) : IServiceBusSenderFactory
{
	private readonly IConnection _connection = connection ?? throw new NullReferenceException(nameof(Exception));
	//protected bool IsConnected => Connection is { IsOpen: true };
}