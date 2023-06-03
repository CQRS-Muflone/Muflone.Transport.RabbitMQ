# Muflone Transport RabbitMQ
Muflone extension to manage queues, and topics on RabbitMQ. 
 
### Install ###
`Install-Package Muflone.Transport.RabbitMQ`


### Implementation ###
It's very simple to register RabbitMQ's transport

    public IServiceCollection RegisterModule(WebApplicationBuilder builder)
    {
        var rabbitMQConfiguration = new RabbitMQConfiguration("localhost", "myuser", "mypassword", "ExchangeCommandsName", "ExchangeForEventsName");
        var connectionFactory = new MufloneConnectionFactory(rabbitMQConfiguration, MyLoggerFactory);
    
        builder.Services.AddScoped<ICommandHandlerAsync<CreateOrder>, CreateOrderCommandHandler>();
        builder.Services.AddScoped<IDomainEventHandlerAsync<OrderCreated>, OrderCreatedEventHandler>();
    
        builder.Services.AddMufloneTransportRabbitMQ(loggerFactory, rabbitMQConfiguration);

        //We need to call a build if we need the service bus or other things registered in Muflone.RabbitMQ
        serviceProvider = builder.Services.BuildServiceProvider();
        builder.Services.AddMufloneRabbitMQConsumers(new List<IConsumer>
        {
            new BeersReceivedConsumer(serviceProvider.GetRequiredService<IServiceBus>(), connectionFactory,	MyLoggerFactory),
            new CreateOrderConsumer(connectionFactory, MyLoggerFactory),
            new OrderCreatedConsumer(connectionFactory, MyLoggerFactory)
        };
        return builder.Services;
    }

### Fully working example
You can find a fully working example here: https://github.com/BrewUp/