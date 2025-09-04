using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Muflone.Transport.RabbitMQ.AppTests;

public class RunTest(IServiceProvider serviceProvider) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await PublishEvent(serviceProvider);
	}

	static async Task PublishEvent(IServiceProvider serviceProvider)
	{
		var orderDate = DateTime.UtcNow;
		var eventBus = serviceProvider.GetRequiredService<IEventBus>();
		var orderCreated = new OrderCreated(new OrderId(Guid.NewGuid()),
				$"{orderDate.Year:0000}{orderDate.Month:00}{orderDate.Day:00}-01");

		await eventBus.PublishAsync(orderCreated, CancellationToken.None);
	}
}