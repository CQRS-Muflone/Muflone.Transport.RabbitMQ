using Muflone.Messages.Commands;
using Muflone.Messages.Events;

namespace Muflone.Transport.RabbitMQ.Abstracts;

public interface IConsumer
{
	Task StartAsync(CancellationToken cancellationToken = default);
	Task StopAsync(CancellationToken cancellationToken = default);
}

public interface IDomainEventConsumer<in T> : IConsumer where T : DomainEvent
{
	Task ConsumeAsync(T message, CancellationToken cancellationToken = default);
}

public interface IIntegrationEventConsumer<in T> : IConsumer where T : IntegrationEvent
{
	Task ConsumeAsync(T message, CancellationToken cancellationToken = default);
}

public interface ICommandConsumer<in T> : IConsumer where T : Command
{
	Task ConsumeAsync(T message, CancellationToken cancellationToken = default);
}

public interface ICommandSender<in T> : IConsumer where T : Command
{
}