using RabbitMQ.Client;
using Eaat.Models;
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
            if (order is null) throw new ArgumentNullException(nameof(order));

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(order));
    
            // Use the RestaurantId as the routing key so the direct exchange routes to the correct restaurant queue
            await _channel.BasicPublishAsync(
                exchange: "order.placed",
                routingKey: order.RestaurantId.ToString(),
                body: body
            );
        }
    }
}
