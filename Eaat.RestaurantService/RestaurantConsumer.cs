using Eaat.Models;
using Eaat.RabbitMQService;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Eaat.RestaurantService
{
    public class RestaurantConsumer
    {
        private readonly RabbitMQConnection _connection;
        private readonly Guid _restaurantId;

        public RestaurantConsumer(RabbitMQConnection connection, Guid restaurantId)
        {
            _connection = connection;
            _restaurantId = restaurantId;
        }

        public async Task StartListeningAsync()
        {
            var channel = _connection.Channel;

            await channel.ExchangeDeclareAsync(
                exchange: "order.placed",
                type: ExchangeType.Direct,
                durable: true);

            var queueName = $"restaurant.{_restaurantId}";

            await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(queueName, "order.placed", _restaurantId.ToString());

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var order = JsonSerializer.Deserialize<OrderPlaced>(Encoding.UTF8.GetString(ea.Body.ToArray()));
                await HandleOrderAsync(channel, order, ea.Body);
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);
        }

        private async Task HandleOrderAsync(IChannel channel, OrderPlaced? order, ReadOnlyMemory<byte> body)
        {
            if (order is null) return;

            Console.WriteLine($"Restaurant {_restaurantId} received order {order.OrderId} - accepting...");

            // Notify couriers via fanout
            await channel.BasicPublishAsync(
                exchange: "order.accepted",
                routingKey: string.Empty,
                body: body);

            Console.WriteLine($"Restaurant {_restaurantId} accepted order {order.OrderId}");
            Console.WriteLine($"Your food for order {order.OrderId} is being prepared");
        }
    }
}