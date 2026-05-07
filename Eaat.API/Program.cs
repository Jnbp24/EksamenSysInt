using Eaat.CourierService;
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

            var rabbitConnection = await RabbitMQConnection.CreateAsync();
            builder.Services.AddSingleton(rabbitConnection);

            builder.Services.AddDbContextFactory<CourierDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("Default")));


            builder.Services.AddSingleton<RabbitMQPublisher>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CourierDbContext>>();

                await using var db = await dbFactory.CreateDbContextAsync();

                db.Database.EnsureCreated();
            }

            var restaurantIds = new[]
            {
                Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"),
                Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae8"),
                Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae9")
            };

            foreach (var id in restaurantIds)
            {
                var conn = await RabbitMQConnection.CreateAsync();

                var restaurant = new RestaurantConsumer(conn, id);
                await restaurant.StartListeningAsync();
            }


            var courierFactory = app.Services.GetRequiredService<IDbContextFactory<CourierDbContext>>();

            var courierConnections = new[]
            {
                await RabbitMQConnection.CreateAsync(),
                await RabbitMQConnection.CreateAsync(),
                await RabbitMQConnection.CreateAsync()
            };

            var couriers = new[]
            {
                new CourierConsumer(courierConnections[0], courierFactory, "Courier 1"),
                new CourierConsumer(courierConnections[1], courierFactory, "Courier 2"),
                new CourierConsumer(courierConnections[2], courierFactory, "Courier 3")
            };

            foreach (var courier in couriers)
            {
                await courier.StartListeningAsync();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}