using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class RabbitMQConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMQConsumer> _logger;
    private IConnection _connection;
    private IChannel _channel;

    public RabbitMQConsumer(IConfiguration configuration, ILogger<RabbitMQConsumer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"],
            Port = int.Parse(_configuration["RabbitMQ:Port"]),
            UserName = _configuration["RabbitMQ:Username"],
            Password = _configuration["RabbitMQ:Password"]
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: "order-queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            RabbitMQProducer producer = new RabbitMQProducer(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build());
            await producer.SendMessageAsync(message, "order-fail-queue");
            _logger.LogInformation($"Mesaj alındı: {message}");
        };

        await _channel.BasicConsumeAsync(
            queue: "order-queue",
            autoAck: true,
            consumer: consumer
        );

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // consumer burada pasif olarak dinlemede
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _channel.CloseAsync();
        _connection?.CloseAsync();
        base.Dispose();
    }
}
