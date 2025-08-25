namespace OrderService.Services.RabbitMQ
{
    public interface IRabbitMQProducer
    {
        public void SendProductMessage<T>(string queueName, T message);
    }
}
