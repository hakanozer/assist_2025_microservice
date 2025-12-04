using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Order.API.Models;
using Order.API.utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
    
namespace Order.API.Saga
{

// 5. Compensation Consumer (Rollback için)
public class CompensationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private IConnection? _connection;
    private IChannel? _channel;

    public CompensationConsumer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        // Payment başarısız olduğunda bu queue'ya mesaj gelecek
        await _channel.QueueDeclareAsync(
            queue: "payment-failed",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var paymentFailed = JsonSerializer.Deserialize<PaymentFailedEvent>(message);

            if (paymentFailed != null)
            {
                await CompensateOrder(paymentFailed.SagaId, paymentFailed.Reason);
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await _channel.BasicConsumeAsync(
            queue: "payment-failed",
            autoAck: false,
            consumer: consumer
        );

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task CompensateOrder(Guid sagaId, string reason)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        using var transaction = await context.Database.BeginTransactionAsync();
        
        try
        {
            // Saga'yı bul
            var saga = await context.OrderSagas.FindAsync(sagaId);
            if (saga == null) return;

            // Order'ı iptal et (soft delete veya status update)
            var order = await context.OrderSagas.FindAsync(saga.OrderId);
            if (order != null)
            {
                order.State = SagaState.OrderCancelled; // Order entity'nizde Status field olmalı
                // veya: context.Orders.Remove(order); // Hard delete
            }

            // Saga state güncelle
            saga.State = SagaState.OrderCancelled;
            saga.CompletedAt = DateTime.UtcNow;
            saga.FailureReason = reason;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            Console.WriteLine($"✅ Order {saga.OrderId} compensated (cancelled) due to: {reason}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"❌ Compensation failed: {ex.Message}");
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}

// 6. Event modelleri
public class PaymentFailedEvent
{
    public Guid SagaId { get; set; }
    public int OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class CreateOrderRequest
{
    public string Product { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// 7. Order entity'nize Status ekleyin
public class Order
{
    public int Id { get; set; }
    public string Product { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = "Pending"; // YENİ!
    public DateTime CreatedAt { get; set; }
}

}
