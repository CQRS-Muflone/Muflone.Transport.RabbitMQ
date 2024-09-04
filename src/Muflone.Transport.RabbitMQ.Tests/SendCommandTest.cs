using Microsoft.Extensions.Logging.Abstractions;
using Muflone.Transport.RabbitMQ.Factories;
using Muflone.Transport.RabbitMQ.Models;

namespace Muflone.Transport.RabbitMQ.Tests;

public class SendCommandTest
{
	[Fact]
	public async Task Can_Send_Command()
	{
		var rabbitMQConfiguration =
			new RabbitMQConfiguration("localhost", "guest", "guest", "MufloneCommands", "MufloneEvents", "Test");
		var mufloneConnectionFactory = new RabbitMQConnectionFactory(rabbitMQConfiguration, new NullLoggerFactory());

		var serviceBus = new ServiceBus(mufloneConnectionFactory, new NullLoggerFactory());
		var createOrder = new CreateOrder(new OrderId(Guid.NewGuid()), "20221020-01");
		await serviceBus.SendAsync(createOrder, CancellationToken.None);
	}

	//[Fact]
	//public async Task Can_Handle_Command()
	//{
	//}
}