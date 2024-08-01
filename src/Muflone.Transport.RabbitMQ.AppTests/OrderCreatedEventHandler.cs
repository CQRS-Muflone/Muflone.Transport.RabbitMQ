using Microsoft.Extensions.Logging;
using Muflone.Messages.Events;

namespace Muflone.Transport.RabbitMQ.AppTests;

public class OrderCreatedEventHandler(ILoggerFactory loggerFactory)
    : DomainEventHandlerAsync<OrderCreated>(loggerFactory)
{
    public override Task HandleAsync(OrderCreated @event, CancellationToken cancellationToken = new ())
    {
        return Task.CompletedTask;
    }
}