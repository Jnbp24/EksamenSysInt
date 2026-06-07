using Eaat.Database;
using Eaat.Models;
using Eaat.RabbitMQService;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Eaat.CourierService
{
    public class CourierConsumer
    {
        private readonly RabbitMQConnection _connection;
        private readonly IDbContextFactory<EaatDbContext> _dbFactory;
        private readonly string _courierName;

        public CourierConsumer(
            RabbitMQConnection connection,
            IDbContextFactory<EaatDbContext> dbFactory,
            string courierName)
        {
            _connection = connection;
            _dbFactory = dbFactory;
            _courierName = courierName;
        }

        public async Task StartListeningAsync()
        {
            var channel = _connection.Channel;

            var notifyQueue = $"courier.notifications.{_courierName}";

            await channel.QueueDeclareAsync(notifyQueue, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(notifyQueue, "order.accepted", string.Empty);
            await channel.QueueDeclareAsync("delivery.claim", durable: true, exclusive: false, autoDelete: false);

            var notifyConsumer = new AsyncEventingBasicConsumer(channel);
            notifyConsumer.ReceivedAsync += async (model, ea) =>
            {
                var order = JsonSerializer.Deserialize<OrderPlaced>(Encoding.UTF8.GetString(ea.Body.ToArray()));
                await HandleNotifyAsync(channel, order, ea.Body);
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            await channel.BasicConsumeAsync(notifyQueue, false, notifyConsumer); // Start consuming notifications for this courier

            var claimConsumer = new AsyncEventingBasicConsumer(channel);
            claimConsumer.ReceivedAsync += async (model, ea) =>
            {
                var order = JsonSerializer.Deserialize<OrderPlaced>(Encoding.UTF8.GetString(ea.Body.ToArray()));
                await HandleClaimAsync(order);
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            await channel.BasicConsumeAsync("delivery.claim", false, claimConsumer); // Start competing for delivery claims - første til mølle
        }

        private async Task HandleNotifyAsync(IChannel channel, OrderPlaced? order, ReadOnlyMemory<byte> body)
        {
            if (order is null) return;

            Console.WriteLine($"{_courierName} notified of order {order.OrderId}");

            // Publish to shared claim queue so couriers fight over the order - først til mølle
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: "delivery.claim",
                body: body);
        }

        private async Task HandleClaimAsync(OrderPlaced? order)
        {
            if (order is null) return;

            await using var db = await _dbFactory.CreateDbContextAsync();

            
            db.OrderClaims.Add(new OrderClaim
            {
                OrderId = order.OrderId,
                CourierName = _courierName,
                ClaimedAt = DateTime.UtcNow
            });

            try
            {
                await db.SaveChangesAsync();
                // Only one courier will ever reach this line
                Console.WriteLine($"{_courierName} CLAIMED order {order.OrderId}!");
                Console.WriteLine($"{order.OrderId} is now being delivered!");
            }
            catch (DbUpdateException)
            {
                // Unique index rejected the duplicate - this courier lost the race
                Console.WriteLine($"{_courierName} was too slow to pick up {order.OrderId}");
            }
        }
    }
}