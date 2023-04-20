namespace Muflone.Transport.RabbitMQ.Models;

public class RabbitMQConfiguration
{
	public readonly string HostName;
	public readonly string VirtualHost;
	public readonly string UserName;
	public readonly string Password;
	public readonly string ClientId;

	//gets the delay for message re-enqueuing.
	public readonly TimeSpan RetryDelay;

	public RabbitMQConfiguration(string hostName, string userName, string password, string clientId)
		: this(hostName, userName, password, clientId, TimeSpan.FromSeconds(30))
	{
	}

	public RabbitMQConfiguration(string hostName, string userName, string password, string clientId, TimeSpan retryDelay)
		: this(hostName, null, userName, password, clientId, retryDelay)
	{
	}

	public RabbitMQConfiguration(string hostName, string vhost, string userName, string password, string clientId,
		TimeSpan retryDelay)
	{
		HostName = hostName;
		UserName = userName;
		Password = password;
		ClientId = clientId;

		RetryDelay = retryDelay;

		VirtualHost = string.IsNullOrWhiteSpace(vhost) ? "/" : vhost;
	}
}