using Eaat.Models;
using Eaat.Resilience;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Eaat.RabbitMQService
{
    public class RabbitMQPublisher
    {
        private readonly IChannel _channel;

        public RabbitMQPublisher(RabbitMQConnection connection)
        {
            _channel = connection.Channel;
        }

        public async Task PublishOrderPlacedAsync(OrderPlaced order)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(order));

            // Use RabbitMQ pipeline - retry, circuit breaker, timeout
            await ResiliencePipelines.RabbitMQ.ExecuteAsync(async ct =>
            {
                await _channel.BasicPublishAsync(
                    exchange: "order.placed",
                    routingKey: order.RestaurantId.ToString(),
                    body: body
                );
            });
        }

        public async Task PublishRawAsync(string exchange, string routingKey, byte[] body)
        {
            await ResiliencePipelines.RabbitMQ.ExecuteAsync(async ct =>
            {
                await _channel.BasicPublishAsync(
                    exchange: exchange,
                    routingKey: routingKey,
                    body: body
                );
            });
        }
    }
}