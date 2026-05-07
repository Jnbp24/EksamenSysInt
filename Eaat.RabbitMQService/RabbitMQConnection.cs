using RabbitMQ.Client;

namespace Eaat.RabbitMQService
{
    public class RabbitMQConnection
    {
        private readonly IConnection _connection;
        public IChannel Channel { get; private set; } // Set property so publisher can grab it

        private RabbitMQConnection(IConnection connection, IChannel channel)
        {
            _connection = connection;
            Channel = channel;
        }

        public static async Task<RabbitMQConnection> CreateAsync()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            // Queue for when an order is placed
            await channel.ExchangeDeclareAsync(
                exchange: "order.placed",
                type: ExchangeType.Direct,
                durable: true
            );

            // Queue for when an order is accepted by the restaurant
            await channel.ExchangeDeclareAsync(
                exchange: "order.accepted",
                type: ExchangeType.Fanout,
                durable: true
            );

            // Direct queue - only one courier can claim the delivery
            await channel.QueueDeclareAsync("delivery.claim", durable: true, exclusive: false, autoDelete: false);

            return new RabbitMQConnection(connection, channel);
        }
    }
}