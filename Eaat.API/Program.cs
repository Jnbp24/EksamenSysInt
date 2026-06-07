using Eaat.CourierService;
using Eaat.Database;
using Eaat.RabbitMQService;
using Eaat.RestaurantService;
using Microsoft.EntityFrameworkCore;

namespace Eaat.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddDbContextFactory<EaatDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

            var rabbitConnection = await RabbitMQConnection.CreateAsync();
            builder.Services.AddSingleton(rabbitConnection);
            builder.Services.AddSingleton<RabbitMQPublisher>();
            builder.Services.AddHostedService<OutboxProcessor>();

            var app = builder.Build();

            await SetupDatabaseAsync(app);
            await SetupRestaurantsAsync();
            await SetupCouriersAsync(app);

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }

        private static async Task SetupDatabaseAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<EaatDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }

        private static async Task SetupRestaurantsAsync()
        {
            var restaurantIds = new[]
            {
                Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"),
                Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae8"),
                Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae9")
            };

            foreach (var id in restaurantIds)
            {
                var connection = await RabbitMQConnection.CreateAsync();
                var restaurant = new RestaurantConsumer(connection, id);
                await restaurant.StartListeningAsync();
            }
        }

        private static async Task SetupCouriersAsync(WebApplication app)
        {
            var courierFactory = app.Services.GetRequiredService<IDbContextFactory<EaatDbContext>>();

            var couriers = new[]
            {
                new CourierConsumer(await RabbitMQConnection.CreateAsync(), courierFactory, "Courier 1"),
                new CourierConsumer(await RabbitMQConnection.CreateAsync(), courierFactory, "Courier 2"),
                new CourierConsumer(await RabbitMQConnection.CreateAsync(), courierFactory, "Courier 3")
            };

            foreach (var courier in couriers)
            {
                await courier.StartListeningAsync();
            }
        }
    }
}