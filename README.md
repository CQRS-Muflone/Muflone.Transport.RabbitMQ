# Muflone.Transport.RabbitMQ

[![NuGet](https://img.shields.io/nuget/v/Muflone.Transport.RabbitMQ.svg)](https://www.nuget.org/packages/Muflone.Transport.RabbitMQ/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Muflone extension to manage queues and topics on RabbitMQ, designed for CQRS and Event Sourcing architectures.

## Features

- **CQRS Support** - Separate commands (Direct exchange) and events (Topic exchange) with dedicated routing
- **Built-in Resilience** - Automatic retry policies via [Polly](https://github.com/App-vNext/Polly) for both connections and message publishing
- **OpenTelemetry Tracing** - Distributed tracing with W3C Trace Context propagation across service boundaries
- **Durable Messaging** - Exchanges, queues, and messages are persisted to disk for reliability
- **Async-first** - All operations use async/await
- **.NET 10** - Built for `net10.0` with nullable reference types

## Install

```
dotnet add package Muflone.Transport.RabbitMQ
```

or

```
Install-Package Muflone.Transport.RabbitMQ
```

## Architecture Overview

The library uses two RabbitMQ exchange types to separate concerns:

| Concept    | Exchange Type | Routing        | Queue Behavior                    |
|------------|---------------|----------------|-----------------------------------|
| Commands   | Direct        | One-to-one     | Exclusive, durable                |
| Events     | Topic         | One-to-many    | Shared across consumers, durable  |

**Commands** are sent to a Direct exchange and delivered to a single consumer (point-to-point).
**Domain Events** and **Integration Events** are published to a Topic exchange and can be consumed by multiple subscribers (pub/sub).

## Quick Start

### 1. Define your messages

Commands and events must extend the Muflone base classes:

```csharp
public class CreateOrder : Command
{
    public readonly string OrderNumber;

    public CreateOrder(OrderId aggregateId, string orderNumber)
        : base(aggregateId)
    {
        OrderNumber = orderNumber;
    }
}

public class OrderCreated : DomainEvent
{
    public readonly string OrderNumber;

    public OrderCreated(OrderId aggregateId, string orderNumber)
        : base(aggregateId)
    {
        OrderNumber = orderNumber;
    }
}
```

### 2. Create command and event handlers

```csharp
public class CreateOrderCommandHandler : ICommandHandlerAsync<CreateOrder>
{
    private readonly IRepository _repository;

    public CreateOrderCommandHandler(IRepository repository)
    {
        _repository = repository;
    }

    public async Task HandleAsync(CreateOrder command, CancellationToken cancellationToken = default)
    {
        // Handle the command: load aggregate, apply business logic, persist
        var order = Order.CreateOrder(command.AggregateId, command.OrderNumber);
        await _repository.SaveAsync(order, Guid.NewGuid(), cancellationToken);
    }
}

public class OrderCreatedEventHandler : IDomainEventHandlerAsync<OrderCreated>
{
    private readonly ILogger _logger;

    public OrderCreatedEventHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType());
    }

    public Task HandleAsync(OrderCreated @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Order created: {OrderNumber}", @event.OrderNumber);
        return Task.CompletedTask;
    }
}
```

### 3. Create consumers

Consumers wire messages to their handlers. Extend the appropriate base class:

```csharp
// Command consumer (one handler per command)
public class CreateOrderConsumer : CommandConsumerBase<CreateOrder>
{
    protected override ICommandHandlerAsync<CreateOrder> HandlerAsync { get; }

    public CreateOrderConsumer(
        IRepository repository,
        IRabbitMQConnectionFactory connectionFactory,
        ILoggerFactory loggerFactory)
        : base(repository, connectionFactory, loggerFactory)
    {
        HandlerAsync = new CreateOrderCommandHandler(repository);
    }
}

// Domain event consumer (supports multiple handlers per event)
public class OrderCreatedConsumer : DomainEventsConsumerBase<OrderCreated>
{
    protected override IEnumerable<IDomainEventHandlerAsync<OrderCreated>> HandlersAsync { get; }

    public OrderCreatedConsumer(
        IRabbitMQConnectionFactory connectionFactory,
        ILoggerFactory loggerFactory)
        : base(connectionFactory, loggerFactory)
    {
        HandlersAsync = new List<IDomainEventHandlerAsync<OrderCreated>>
        {
            new OrderCreatedEventHandler(loggerFactory)
        };
    }
}

// Integration event consumer (for cross-boundary events)
public class OrderShippedConsumer : IntegrationEventsConsumerBase<OrderShipped>
{
    protected override IEnumerable<IIntegrationEventHandlerAsync<OrderShipped>> HandlersAsync { get; }

    public OrderShippedConsumer(
        IRabbitMQConnectionFactory connectionFactory,
        ILoggerFactory loggerFactory)
        : base(connectionFactory, loggerFactory)
    {
        HandlersAsync = new List<IIntegrationEventHandlerAsync<OrderShipped>>
        {
            new OrderShippedIntegrationHandler(loggerFactory)
        };
    }
}
```

### 4. Register services in DI

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure RabbitMQ connection
var rabbitMQConfiguration = new RabbitMQConfiguration(
    hostName: "localhost",
    userName: "guest",
    password: "guest",
    exchangeCommandsName: "Muflone.Commands",
    exchangeEventsName: "Muflone.Events",
    clientId: "OrderService"
);

// Register handlers
builder.Services.AddScoped<ICommandHandlerAsync<CreateOrder>, CreateOrderCommandHandler>();
builder.Services.AddScoped<IDomainEventHandlerAsync<OrderCreated>, OrderCreatedEventHandler>();

// Register Muflone RabbitMQ transport
builder.Services.AddMufloneTransportRabbitMQ(loggerFactory, rabbitMQConfiguration);

```

### 5. Send commands and publish events

```csharp
// Inject IServiceBus to send commands
public class OrderController : ControllerBase
{
    private readonly IServiceBus _serviceBus;

    public OrderController(IServiceBus serviceBus)
    {
        _serviceBus = serviceBus;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        var command = new CreateOrder(
            new OrderId(Guid.NewGuid()),
            request.OrderNumber);

        await _serviceBus.SendAsync(command);
        return Accepted();
    }
}

// Inject IEventBus to publish events
public class OrderService
{
    private readonly IEventBus _eventBus;

    public OrderService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task NotifyOrderCreated(OrderId orderId, string orderNumber)
    {
        var @event = new OrderCreated(orderId, orderNumber);
        await _eventBus.PublishAsync(@event);
    }
}
```

## Configuration

`RabbitMQConfiguration` supports several constructor overloads:

```csharp
// Basic (default retry delay: 30s, default vhost: "/")
new RabbitMQConfiguration(hostName, userName, password,
    exchangeCommandsName, exchangeEventsName, clientId);

// With custom retry delay
new RabbitMQConfiguration(hostName, userName, password,
    TimeSpan.FromSeconds(60),
    exchangeCommandsName, exchangeEventsName, clientId);

// With virtual host and custom retry delay
new RabbitMQConfiguration(hostName, "/my-vhost", userName, password,
    TimeSpan.FromSeconds(60),
    exchangeCommandsName, exchangeEventsName, clientId);
```

| Parameter              | Description                                          | Default |
|------------------------|------------------------------------------------------|---------|
| `hostName`             | RabbitMQ server hostname                             | -       |
| `vhost`                | RabbitMQ virtual host                                | `"/"`   |
| `userName`             | Authentication username                              | -       |
| `password`             | Authentication password                              | -       |
| `retryDelay`           | Delay between connection retry attempts              | 30s     |
| `exchangeCommandsName` | Name of the Direct exchange for commands             | -       |
| `exchangeEventsName`   | Name of the Topic exchange for events                | -       |
| `clientId`             | Unique identifier for this service (used in queues)  | -       |

## OpenTelemetry Integration

The library includes built-in distributed tracing using `System.Diagnostics.ActivitySource`. Trace context is automatically propagated through RabbitMQ message headers using the W3C Trace Context standard (`traceparent` / `tracestate`).

### Recommended: Using Muflone.OpenTelemetry

The easiest way to enable full distributed tracing is to install the [Muflone.OpenTelemetry](https://www.nuget.org/packages/Muflone.OpenTelemetry/) package:

```
dotnet add package Muflone.OpenTelemetry
```

Then register it with just two lines of code:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddMufloneInstrumentation()  // registers all Muflone activity sources
        .AddOtlpExporter());

builder.Services.AddMufloneOpenTelemetry();  // decorates IServiceBus, IEventBus, and IRepository with instrumented wrappers
```

This automatically instruments command handlers, event handlers, service bus, event bus, and repository operations with zero changes to your existing code.

### Manual Configuration

If you prefer to configure tracing manually without the `Muflone.OpenTelemetry` package, you can register the activity sources individually:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Muflone.ServiceBus")
        .AddSource("Muflone.EventBus")
        .AddOtlpExporter());
```

Two activity sources are available:

| Activity Source       | Operations      |
|-----------------------|-----------------|
| `Muflone.ServiceBus`  | Command `send`  |
| `Muflone.EventBus`    | Event `publish` |

You can also pass trace context manually through `UserProperties` when sending commands or publishing events from within a handler:

```csharp
var command = new CreateOrder(aggregateId, orderNumber);
command.UserProperties["traceparent"] = Activity.Current?.Id;
command.UserProperties["tracestate"] = Activity.Current?.TraceStateString;

await serviceBus.SendAsync(command);
```

## Resilience

The library uses [Polly](https://github.com/App-vNext/Polly) for resilience at two levels:

- **Connection**: 5 retries with exponential backoff on connection failures, with automatic reconnection on shutdown or channel exceptions
- **Message publishing**: 3 retries with exponential backoff (2, 4, 8 seconds) for both `SendAsync` and `PublishAsync`

## Registered Services

`AddMufloneTransportRabbitMQ` registers the following services as singletons:

| Interface                 | Implementation              |
|---------------------------|-----------------------------|
| `IRabbitMQConnectionFactory` | `RabbitMQConnectionFactory` |
| `IServiceBus`             | `ServiceBus`                |
| `IEventBus`               | `ServiceBus`                |
| `IMessageSubscriber`      | `RabbitMQSubscriber`        |
| `IHostedService`          | `RabbitMQStarter`           |
| `IHostedService`          | `MessageHandlersStarter`    |

## Fully working example

You can find a fully working example here: https://github.com/BrewUp/

## License

This project is licensed under the [MIT License](https://opensource.org/licenses/MIT).
