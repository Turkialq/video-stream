namespace Services.RabbitManager
{
    public interface IRabbitManager
    {
        void Publish(string exchange, string routingKey, byte[] body);
    }
}
