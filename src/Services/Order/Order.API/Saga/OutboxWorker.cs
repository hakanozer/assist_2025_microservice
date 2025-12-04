using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Order.API.utils;

namespace Order.API.Saga
{
    public class OutboxWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public OutboxWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Her iterasyonda yeni bir scope oluştur
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var messages = await context.OutboxMessages
                    .Where(x => !x.Sent)
                    .OrderBy(x => x.CreatedAt)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                if (!messages.Any())
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                var factory = new ConnectionFactory
                {
                    HostName = "localhost",
                    UserName = "guest",
                    Password = "guest"
                };

                await using var connection = await factory.CreateConnectionAsync();
                await using var channel = await connection.CreateChannelAsync();

                // BasicProperties artık doğrudan new ile oluşturuluyor
                var props = new BasicProperties
                {
                    ContentType = "application/json",
                    DeliveryMode = DeliveryModes.Persistent
                };

                foreach (var m in messages)
                {
                    var body = Encoding.UTF8.GetBytes(m.Payload);

                    await channel.BasicPublishAsync(
                        exchange: "",
                        routingKey: "order-created",
                        mandatory: false,
                        basicProperties: props,
                        body: body,
                        cancellationToken: stoppingToken
                    );

                    m.Sent = true;
                }

                await context.SaveChangesAsync(stoppingToken);
            }
        }
    }
}