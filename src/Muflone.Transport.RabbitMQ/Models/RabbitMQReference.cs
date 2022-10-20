namespace Muflone.Transport.RabbitMQ.Models;

public record RabbitMQReference(string ExchangeCommandsName, string QueueCommandsName, string ExchangeEventsName,
    string QueueEventsName)
{
    public string DeadLetterExchangeName { get; init; } = $"{ExchangeCommandsName}.dead";
    public string DeadLetterQueueName { get; init; } = $"{QueueCommandsName}.dead";
}
