using Microsoft.Extensions.Logging;
using Muflone.Core;
using Muflone.Messages.Commands;
using Muflone.Messages.Events;
using Muflone.Persistence;
using Muflone.Transport.RabbitMQ.Abstracts;
using Muflone.Transport.RabbitMQ.Consumers;
using Muflone.Transport.RabbitMQ.Models;

namespace Muflone.Transport.RabbitMQ.Tests;

public class OrderId : DomainId
{
    public OrderId(Guid value) : base(value)
    {
    }
}

public class CreateOrder : Command
{
    public readonly OrderId OrderId;
    public readonly string OrderNumber;

    public CreateOrder(OrderId aggregateId, string orderNumber) : base(aggregateId)
    {
        OrderId = aggregateId;
        OrderNumber = orderNumber;
    }
}

public class OrderCreated : DomainEvent
{
    public readonly OrderId OrderId;
    public readonly string OrderNumber;

    public OrderCreated(OrderId aggregateId, string orderNumber) : base(aggregateId)
    {
        OrderId = aggregateId;
        OrderNumber = orderNumber;
    }
}

public class OrderCreatedConsumer : DomainEventsConsumerBase<OrderCreated>
{
    public OrderCreatedConsumer(RabbitMQReference rabbitMQReference,
        IMufloneConnectionFactory mufloneConnectionFactory,
        ILoggerFactory loggerFactory,
        ISerializer? messageSerializer = null) : base(rabbitMQReference, mufloneConnectionFactory, loggerFactory, messageSerializer)
    {
    }

    protected override IEnumerable<IDomainEventHandlerAsync<OrderCreated>> HandlersAsync { get; }
}