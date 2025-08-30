using RabbitMQ.Client;
namespace OrderService.Services.RabbitMQ
{
    public class RabbitMQProducer : IRabbitMQProducer, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQProducer> _logger;
        public async void SendProductMessage<T>(string queueName, T message)
        {
            _logger.LogInformation("Sending message to RabbitMQ queue: {QueueName}", queueName);
            var factory = new ConnectionFactory()
            {
                HostName = "localhost", // Now _configuration is available
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };
            _logger.LogInformation("RabbitMQ connection factory created with Host: {Host}, Port: {Port}", factory.HostName, factory.Port);
            using var connection = await factory.CreateConnectionAsync();
            _logger.LogInformation("RabbitMQ connection established.");
            using var channel = await connection.CreateChannelAsync();
            _logger.LogInformation("RabbitMQ channel created.");

            await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false);
            _logger.LogInformation("RabbitMQ queue declared: {QueueName}", queueName);
            var body = System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));
            _logger.LogInformation("Message serialized and encoded to byte array.");
            await channel.BasicPublishAsync(exchange: "", routingKey: queueName, body: body);
            _logger.LogInformation("Message published to RabbitMQ queue: {QueueName}", queueName);
        }
        public void Dispose()
        {
            // Dispose of any resources if necessary
        }
    }
}
