namespace Muflone.Transport.RabbitMQ.Models;

public class RabbitMQConfiguration
{
	public readonly string HostName;
	public readonly string VirtualHost;
	public readonly string UserName;
	public readonly string Password;

	public readonly string ExchangeCommandsName;
	public readonly string ExchangeEventsName;

	public readonly string ClientId;

	//gets the delay for message re-enqueuing.
	public readonly TimeSpan RetryDelay;

	public RabbitMQConfiguration(string hostName, string userName, string password, string exchangeCommandsName, string exchangeEventsName, string clientId)
		: this(hostName, userName, password, exchangeCommandsName, exchangeEventsName, clientId, TimeSpan.FromSeconds(30)) { }

	public RabbitMQConfiguration(string hostName, string userName, string password, string exchangeCommandsName, string exchangeEventsName, string clientId, TimeSpan retryDelay)
		: this(hostName: hostName, vhost: null, userName: userName, password: password, exchangeCommandsName, exchangeEventsName, clientId, retryDelay) { }

	public RabbitMQConfiguration(string hostName, string vhost, string userName, string password,
		string exchangeCommandsName, string exchangeEventsName, string clientId, TimeSpan retryDelay)
	{
		HostName = hostName;
		UserName = userName;
		Password = password;

		ExchangeCommandsName = exchangeCommandsName;
		ExchangeEventsName = exchangeEventsName;
		ClientId = clientId;

		RetryDelay = retryDelay;

		VirtualHost = string.IsNullOrWhiteSpace(vhost) ? "/" : vhost;
	}
}