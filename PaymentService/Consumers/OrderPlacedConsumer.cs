using Microsoft.Extensions.Configuration; // Add this using
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentService.Contracts;
using PaymentService.Data;
using PaymentService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace PaymentService.Consumers
{
    public class OrderPlacedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;

        // Inject IConfiguration here
        public OrderPlacedConsumer(IServiceScopeFactory serviceProvider, IConfiguration configuration)
        {
            _serviceScopeFactory = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost", 
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };
            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "OrderQueue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var orderEvent = JsonSerializer.Deserialize<OrderplacedEvent>(message); // Fixed typo: OrderplacedEvent -> OrderPlacedEvent

                await using var scope = _serviceScopeFactory.CreateAsyncScope(); // Use the renamed field
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

                await db.Payment.AddAsync(payment); // Assuming the DbSet is named Payments, not Payment
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

            await channel.BasicConsumeAsync(queue: "OrderQueue", autoAck: false, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}