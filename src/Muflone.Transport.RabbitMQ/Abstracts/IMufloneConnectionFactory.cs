using RabbitMQ.Client;

namespace Muflone.Transport.RabbitMQ.Abstracts;

public interface IMufloneConnectionFactory
{
	IConnection Connection { get; }
	IModel CreateChannel();
}