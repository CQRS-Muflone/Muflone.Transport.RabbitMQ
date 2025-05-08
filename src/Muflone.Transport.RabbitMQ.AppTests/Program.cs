// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Muflone;
using Muflone.Transport.RabbitMQ;
using Muflone.Transport.RabbitMQ.AppTests;
using Muflone.Transport.RabbitMQ.Factories;
using Muflone.Transport.RabbitMQ.Models;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

var rabbitMqConfiguration = new RabbitMQConfiguration("localhost", "guest",
		"guest", "Muflone.Commands", "Muflone.Events",
		"Test");
var mufloneConnectionFactory = new RabbitMQConnectionFactory(rabbitMqConfiguration, new NullLoggerFactory());

bool withConsumer = false;

builder.Services.AddMufloneTransportRabbitMQ(new NullLoggerFactory(), rabbitMqConfiguration);
builder.Services.AddHostedService<RunTest>();

if (withConsumer)
{
	var consumers = new List<IConsumer>
	{
		new OrderCreatedConsumer(mufloneConnectionFactory, new NullLoggerFactory())
	};
	builder.Services.AddMufloneRabbitMQConsumers(consumers);	
}
else
{
	builder.Services.AddMessageHandler<OrderCreatedEventHandler>();
}

using IHost host = builder.Build();

await host.RunAsync();