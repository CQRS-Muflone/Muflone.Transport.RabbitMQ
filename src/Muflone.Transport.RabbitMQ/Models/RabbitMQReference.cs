namespace Muflone.Transport.RabbitMQ.Models;

public record RabbitMQReference(string ExchangeName, string QueueName)
{
    public string DeadLetterExchangeName { get; init; } = $"{ExchangeName}.dead";
    public string DeadLetterQueueName { get; init; } = $"{QueueName}.dead";
}
