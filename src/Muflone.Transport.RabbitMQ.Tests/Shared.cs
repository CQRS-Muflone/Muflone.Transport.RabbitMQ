﻿namespace Muflone.Transport.RabbitMQ.Tests;

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

public class OrderCreatedConsumer : DomainEventConsumerBase<OrderCreated>
{
	public OrderCreatedConsumer(RabbitMQReference rabbitMQReference,
		IMufloneConnectionFactory mufloneConnectionFactory,
		ILoggerFactory loggerFactory) : base(rabbitMQReference, mufloneConnectionFactory, loggerFactory)
	{
	}

	protected override IDomainEventHandlerAsync<OrderCreated> HandlerAsync { get; }
}