using Eaat.RabbitMQService;
using Eaat.RestaurantService;
using Microsoft.Extensions.Hosting;

namespace Eaat.CourierService
{
    public class RestaurantHostedService : BackgroundService
    {
        private readonly List<RestaurantConsumer> _restaurants = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var restaurantIds = new[]
            {
                Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"),
                Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae8"),
                Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae9")
            };

            foreach (var restaurantId in restaurantIds)
            {
                var connection = await RabbitMQConnection.CreateAsync();

                var consumer = new RestaurantConsumer(
                    connection,
                    restaurantId);

                _restaurants.Add(consumer);

                await consumer.StartListeningAsync();
            }

            // Keep service alive
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}