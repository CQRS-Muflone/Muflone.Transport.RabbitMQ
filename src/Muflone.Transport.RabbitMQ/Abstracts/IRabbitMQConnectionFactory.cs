using RabbitMQ.Client;

namespace Muflone.Transport.RabbitMQ.Abstracts;

public interface IRabbitMQConnectionFactory : ITransporterConnectionFactory
{
	IConnection Connection { get; }
	IModel CreateChannel();
	string ExchangeCommandsName { get; }
	string ExchangeEventsName { get; }
	string ClientId { get; }
}