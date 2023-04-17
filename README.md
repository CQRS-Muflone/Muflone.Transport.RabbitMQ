# Muflone Transport RabbitMQ
Muflone extension to manage queues, and topics on RabbitMQ. 
 
### Install ###
`Install-Package Muflone.Transport.RabbitMQ`

### Sample ###
It's very simple to register RabbitMQ's transport

    public IServiceCollection RegisterModule(WebApplicationBuilder builder)
    {
        var rabbitMQConfiguration = new RabbitMQConfiguration("localhost", "myuser", "mypassword", "Muflone");
        var rabbitMQReference =
            new RabbitMQReference("MufloneCommands", "CreateOrder", "MufloneEvents", "OrderCreated");
        var mufloneConnectionFactory = new MufloneConnectionFactory(rabbitMQConfiguration, new NullLoggerFactory());
    
        builder.Services.AddScoped<ICommandHandlerAsync<CreateOrder>, CreateOrderCommandHandler>();
        builder.Services.AddScoped<IDomainEventHandlerAsync<OrderCreated>, OrderCreatedEventHandler>();
    
        var conumsers = new List<IConsumer>
        {
            new CreateOrderConsumer(rabbitMQReference, mufloneConnectionFactory, new NullLoggerFactory()),
            new OrderCreatedConsumer(rabbitMQReference, mufloneConnectionFactory, new NullLoggerFactory())
        };
    
        builder.Services.AddMufloneTransportRabbitMQ(rabbitMQConfiguration, rabbitMQReference, conumsers);
    
        return builder.Services;
    }

