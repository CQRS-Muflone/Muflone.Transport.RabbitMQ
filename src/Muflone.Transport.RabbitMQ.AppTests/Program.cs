// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Muflone;
using Muflone.Transport.RabbitMQ;
using Muflone.Transport.RabbitMQ.Abstracts;
using Muflone.Transport.RabbitMQ.AppTests;
using Muflone.Transport.RabbitMQ.Factories;
using Muflone.Transport.RabbitMQ.Models;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

var rabbitMqConfiguration = new RabbitMQConfiguration("localhost", "guest",
    "guest", "Muflone.Commands", "Muflone.Events",
    "Test");
var mufloneConnectionFactory = new MufloneConnectionFactory(rabbitMqConfiguration, new NullLoggerFactory());

builder.Services.AddMufloneTransportRabbitMQ(new NullLoggerFactory(), rabbitMqConfiguration);

var consumers = new List<IConsumer>
{
    new OrderCreatedConsumer(mufloneConnectionFactory, new NullLoggerFactory())
};
builder.Services.AddMufloneRabbitMQConsumers(consumers);


using IHost host = builder.Build();

PublishEvent(host.Services);

await host.RunAsync();


static async Task PublishEvent(IServiceProvider serviceProvider)
{
    var eventBus  = serviceProvider.GetRequiredService<IEventBus>();
    var orderCreated = new OrderCreated(new OrderId(Guid.NewGuid()), "20240801-01");

    await eventBus.PublishAsync(orderCreated, CancellationToken.None);
}