using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.Configuration;


public interface IRabbitMQProducer
{
    Task SendMessageAsync(string message, string queueName);
}

public class RabbitMQProducer : IRabbitMQProducer
{
    private readonly IConfiguration _configuration;

    public RabbitMQProducer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendMessageAsync(string message, string queueName)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"],
            Port = int.Parse(_configuration["RabbitMQ:Port"]),
            UserName = _configuration["RabbitMQ:Username"],
            Password = _configuration["RabbitMQ:Password"],
        };


        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // Queue declare
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, body: body);

    }
}
