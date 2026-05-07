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

            await channel.QueueDeclareAsync(
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await channel.QueueBindAsync(
                queueName,
                "order.placed",
                _restaurantId.ToString());

            Console.WriteLine($"Restaurant {_restaurantId} listening on {queueName}");

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var order = JsonSerializer.Deserialize<OrderPlaced>(
                    Encoding.UTF8.GetString(ea.Body.ToArray()));

                if (order is null)
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                Console.WriteLine($"Restaurant {_restaurantId} received order {order.OrderId} - accepting...");

                // Notify couriers (fanout is correct here)
                await channel.BasicPublishAsync(
                    exchange: "order.accepted",
                    routingKey: string.Empty,
                    body: ea.Body);

                Console.WriteLine($"Restaurant {_restaurantId} accepted order {order.OrderId}");

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);
        }
    }
}