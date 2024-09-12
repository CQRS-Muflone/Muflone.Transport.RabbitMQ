using Microsoft.Extensions.Hosting;

namespace Muflone.Transport.RabbitMQ;

public class RabbitMqStarter : IHostedService
{
	private readonly IEnumerable<IConsumer> _consumers;

	public RabbitMqStarter(IEnumerable<IConsumer> consumers)
	{
		_consumers = consumers;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		foreach (var consumer in _consumers) consumer.StartAsync(cancellationToken);
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}