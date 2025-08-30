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
        private readonly ILogger<OrderPlacedConsumer> _logger;

        // Inject IConfiguration here
        public OrderPlacedConsumer(IServiceScopeFactory serviceProvider, IConfiguration configuration, ILogger<OrderPlacedConsumer> logger)
        {
            _serviceScopeFactory = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting OrderPlacedConsumer...");
            var factory = new ConnectionFactory()
            {
                HostName = "localhost", 
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };
            _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port}", factory.HostName, factory.Port);
            var connection = await factory.CreateConnectionAsync();
            _logger.LogInformation("Connected to RabbitMQ");
            var channel = await connection.CreateChannelAsync();
            _logger.LogInformation("Channel created");

            await channel.QueueDeclareAsync(queue: "OrderQueue", durable: false, exclusive: false, autoDelete: false, arguments: null);
            _logger.LogInformation("Declared queue: OrderQueue");
            var consumer = new AsyncEventingBasicConsumer(channel);
            _logger.LogInformation("Consumer created");
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                _logger.LogInformation("Received message from OrderQueue: {Message}", Encoding.UTF8.GetString(body));
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Deserializing message");
                var orderEvent = JsonSerializer.Deserialize<OrderplacedEvent>(message); // Fixed typo: OrderplacedEvent -> OrderPlacedEvent
                _logger.LogInformation("Deserialized OrderPlacedEvent: {@OrderEvent}", orderEvent);
                await using var scope = _serviceScopeFactory.CreateAsyncScope(); // Use the renamed field
                _logger.LogInformation("Created service scope");
                var db = scope.ServiceProvider.GetRequiredService<PaymentServiceContext>();
                _logger.LogInformation("Obtained PaymentServiceContext from scope");

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
                _logger.LogInformation("Created Payment entity: {@Payment}", payment);
                await db.Payment.AddAsync(payment); // Assuming the DbSet is named Payments, not Payment
                await db.SaveChangesAsync();
                _logger.LogInformation("Saved Payment to database with ID: {PaymentId}", payment.Id);

                // Publish PaymentProcessedEvent
                var paymentEvent = new PaymentProcessedEvent
                {
                    OrderId = orderEvent.OrderId,
                    Items = orderEvent.Items.Select(item =>
                        new OrderItemDTO(item.BookId, item.Quantity, item.UnitPrice)).ToList(),
                    ProcessedAtUtc = DateTime.UtcNow
                };
                _logger.LogInformation("Created PaymentProcessedEvent: {@PaymentEvent}", paymentEvent);
                var paymentMessage = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(paymentEvent));
                _logger.LogInformation("Publishing PaymentProcessedEvent to EditBookCount queue");
                await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "EditBookCount", body: paymentMessage);
                _logger.LogInformation("Published PaymentProcessedEvent to EditBookCount queue");
            };

            await channel.BasicConsumeAsync(queue: "OrderQueue", autoAck: false, consumer: consumer);
            _logger.LogInformation("Started consuming from OrderQueue");
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}