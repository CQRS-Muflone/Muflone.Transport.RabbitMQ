# Muflone Transport RabbitMQ
Muflone extension to manage queues, and topics on RabbitMQ. 
 
### Install ###
`Install-Package Muflone.Transport.RabbitMQ`


### 2023-04-23 Breaking changes
- Renamed `DomainEventsConsumerBase` to `DomainEventConsumerBase`
- Added `IRepository` in the ConsumerBase's constructor
- Now ConsumerBase's `LoggerFactory` is public and not private anymore. so we can use it in something like that:

      public class CreateCartConsumer : CommandConsumerBase<CreateCart>
      {
          public CreateCartConsumer(IRepository repository, RabbitMQReference rabbitMQReference, IMufloneConnectionFactory mufloneConnectionFactory, ILoggerFactory loggerFactory)
          : base(repository, rabbitMQReference, mufloneConnectionFactory, loggerFactory)
          {
  
          }
  
          protected override ICommandHandlerAsync<CreateCart> HandlerAsync => new CreateCartCommandHandler(Repository, LoggerFactory);
      }



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

