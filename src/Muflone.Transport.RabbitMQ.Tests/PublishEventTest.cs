using Microsoft.Extensions.Logging.Abstractions;
using Muflone.Transport.RabbitMQ.Factories;
using Muflone.Transport.RabbitMQ.Models;

namespace Muflone.Transport.RabbitMQ.Tests;

public class PublishEventTest
{
	[Fact]
	public async Task Can_Publish_Event()
	{
		var rabbitMQConfiguration = new RabbitMQConfiguration("localhost", "myuser", "mypassword", "Muflone");
		var rabbitMQReference = new RabbitMQReference("MufloneCommands", "CreateOrder", "MufloneEvents", "OrderCreated");
		var mufloneConnectionFactory = new MufloneConnectionFactory(rabbitMQConfiguration, new NullLoggerFactory());

		var serviceBus = new ServiceBus(mufloneConnectionFactory, rabbitMQReference, new NullLoggerFactory());
		var orderCreated = new OrderCreated(new OrderId(Guid.NewGuid()), "20221020-01");
		await serviceBus.PublishAsync(orderCreated, CancellationToken.None);
	}

	[Fact(Skip = "Not completed")]
	public async Task Can_Handle_Event()
	{
		var rabbitMQConfiguration = new RabbitMQConfiguration("localhost", "myuser", "mypassword", "Muflone");
		var rabbitMQReference = new RabbitMQReference("MufloneCommands", "CreateOrder", "MufloneEvents", "OrderCreated");
		var mufloneConnectionFactory = new MufloneConnectionFactory(rabbitMQConfiguration, new NullLoggerFactory());

		//TODO: Create a MOQ for the servcieprovider
		//var domainEventConsumer = new OrderCreatedConsumer(serviceProvider, rabbitMQReference, mufloneConnectionFactory, new NullLoggerFactory());
		//await domainEventConsumer.StartAsync(CancellationToken.None);
	}
}