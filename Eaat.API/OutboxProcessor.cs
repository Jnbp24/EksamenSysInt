using Eaat.Database;
using Eaat.RabbitMQService;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Eaat.Api
{
    public class OutboxProcessor : BackgroundService
    {
        private readonly IDbContextFactory<EaatDbContext> _dbFactory;
        private readonly RabbitMQPublisher _publisher;

        public OutboxProcessor(
            IDbContextFactory<EaatDbContext> dbFactory,
            RabbitMQPublisher publisher)
        {
            _dbFactory = dbFactory;
            _publisher = publisher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await using var db = await _dbFactory.CreateDbContextAsync(stoppingToken);

                // Fetch all unprocessed outbox messages
                var messages = await db.OutboxMessages
                    .Where(m => m.ProcessedAt == null)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        await _publisher.PublishRawAsync(
                            message.Exchange,
                            message.RoutingKey,
                            Encoding.UTF8.GetBytes(message.Payload)
                        );

                        message.ProcessedAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Something went wrong during the task. Task terminated: {ex.Message}");
                    }
                }

                await db.SaveChangesAsync(stoppingToken);

                // Poll every 5 seconds
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}