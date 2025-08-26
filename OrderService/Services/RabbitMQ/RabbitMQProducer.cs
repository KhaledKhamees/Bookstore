using RabbitMQ.Client;
namespace OrderService.Services.RabbitMQ
{
    public class RabbitMQProducer : IRabbitMQProducer, IDisposable
    {
        private readonly IConfiguration _configuration;
        public async void SendProductMessage<T>(string queueName, T message)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost", // Now _configuration is available
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false);
            var body = System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));
            await channel.BasicPublishAsync(exchange: "", routingKey: queueName, body: body);
        }
        public void Dispose()
        {
            // Dispose of any resources if necessary
        }
    }
}
