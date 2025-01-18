using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using FileUploadService.Models;

namespace Services.RabbitManager
{
    public class RabbitManager : IRabbitManager, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitManager(IOptions<RabbitSettings> rabbitOptions)
        {
            var settings = rabbitOptions.Value;

            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                UserName = settings.UserName,
                Password = settings.Password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(
                exchange: "video.events",
                type: ExchangeType.Topic,
                durable: true
            );
        }

        public void Publish(string exchange, string routingKey, byte[] body)
        {
            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: null,
                body: body
            );
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
