
using CatalogService.Contracts;
using CatalogService.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CatalogService.Consumers
{
    public class PaymentProcessedConsumer:BackgroundService
    {
        private readonly IServiceScopeFactory serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentProcessedConsumer> _logger;
        public PaymentProcessedConsumer(IServiceScopeFactory serviceProvider,IConfiguration configuration, ILogger<PaymentProcessedConsumer> logger)
        {
            this.serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting PaymentProcessedConsumer...");
            var factory = new ConnectionFactory()
            {
                HostName = "localhost", // Now _configuration is available
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };
            _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port}", factory.HostName, factory.Port);
            var connection = await factory.CreateConnectionAsync();
            _logger.LogInformation("Connected to RabbitMQ");
            var channel = await connection.CreateChannelAsync();
            _logger.LogInformation("Channel created");
            await channel.QueueDeclareAsync(queue: "EditBookCount", durable: false, exclusive: false, autoDelete: false, arguments: null);
            _logger.LogInformation("Declared queue 'EditBookCount'");
            var consumer = new AsyncEventingBasicConsumer(channel);
            _logger.LogInformation("Consumer created, waiting for messages...");
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                _logger.LogInformation("Received message: {Message}", Encoding.UTF8.GetString(body));
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Deserializing message to PaymentProcessedEvent");
                var paymentEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(message);
                if (paymentEvent == null)
                {
                    _logger.LogWarning("Received null payment event");
                    return;
                }
                await using var scope = serviceProvider.CreateAsyncScope();
                _logger.LogInformation("Processing payment event for Order ID: {OrderId}", paymentEvent.OrderId);
                var db = scope.ServiceProvider.GetRequiredService<CatalogServiceContext>();
                _logger.LogInformation("Updating book stock based on payment event items");
                foreach (var item in paymentEvent.Items)
                {
                    var book = await db.Book.FindAsync(item.BookId);
                    if (book != null)
                    {
                        book.stock -= item.Quantity;
                    }
                }
                await db.SaveChangesAsync();
                _logger.LogInformation("Book stock updated successfully for Order ID: {OrderId}", paymentEvent.OrderId);
            };
            _logger.LogInformation("Setting up consumer to listen to 'EditBookCount' queue");
            await channel.BasicConsumeAsync(queue: "EditBookCount", autoAck: false, consumer: consumer);
            _logger.LogInformation("Started consuming messages from 'EditBookCount' queue");
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
