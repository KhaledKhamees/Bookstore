
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
        private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration _configuration;
        public PaymentProcessedConsumer(IServiceProvider serviceProvider)
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

            await channel.QueueDeclareAsync(queue: "EditBookCount", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var paymentEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(message);
                await using var scope = serviceProvider.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<CatalogServiceContext>();
                foreach (var item in paymentEvent.Items)
                {
                    var book = await db.Book.FindAsync(item.BookId);
                    if (book != null)
                    {
                        book.stock -= item.Quantity;
                    }
                }
                await db.SaveChangesAsync();
            };
            await channel.BasicConsumeAsync(queue: "EditBookCount", autoAck: true, consumer: consumer);
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
