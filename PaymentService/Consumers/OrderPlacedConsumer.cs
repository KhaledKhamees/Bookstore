using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using PaymentService.Contracts;
using PaymentService.Data;
using PaymentService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PaymentService.Consumers
{
    public class OrderPlacedConsumer : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration _configuration;

        public OrderPlacedConsumer(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:HostName"],
                Port = int.Parse(_configuration["RabbitMQ:Port"]),
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"]
            };
            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "OrderEvent", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var orderEvent = JsonSerializer.Deserialize<OrderplacedEvent>(message);

                await using var scope = serviceProvider.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentServiceContext>();

                // Save payment in DB
                var payment = new Payment
                {
                    OrderId = orderEvent.OrderId,
                    UserId = orderEvent.CustomerId,
                    Amount = orderEvent.TotalPrice,
                    Status = Enums.PaymentStatus.Completed,
                    Method = Enums.PaymentMethod.PayPal,
                    CreatedAt = DateTime.UtcNow
                };

                await db.Payment.AddAsync(payment);
                await db.SaveChangesAsync();

                // Publish PaymentProcessedEvent
                var paymentEvent = new PaymentProcessedEvent
                {
                    OrderId = orderEvent.OrderId,
                    Items = orderEvent.Items.Select(item =>
                        new OrderItemDTO(item.BookId, item.Quantity, item.UnitPrice)).ToList(),
                    ProcessedAtUtc = DateTime.UtcNow
                };

                var paymentMessage = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(paymentEvent));
                await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "EditBookCount", body: paymentMessage);
            };

            await channel.BasicConsumeAsync(queue: "OrderEvent", autoAck: true, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

    }
}