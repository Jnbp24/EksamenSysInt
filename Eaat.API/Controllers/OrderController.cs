using Eaat.Database;
using Eaat.Models;
using Eaat.RabbitMQService;
using Eaat.Resilience;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Eaat.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IDbContextFactory<EaatDbContext> _dbFactory;

        public OrderController(
            IDbContextFactory<EaatDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderPlaced order)
        {
            try
            {
                await ResiliencePipelines.Api.ExecuteAsync(async ct =>
                {
                    await using var db = await _dbFactory.CreateDbContextAsync(ct);

                    // Write order + outbox message atomically
                    await using var transaction =
                        await db.Database.BeginTransactionAsync(ct);

                    db.OutboxMessages.Add(new OutboxMessage
                    {
                        Exchange = "order.placed",
                        RoutingKey = order.RestaurantId.ToString(),
                        Payload = JsonSerializer.Serialize(order)
                    });

                    await db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                });

                return Ok($"Order {order.OrderId} placed successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(503, $"Service unavailable - {ex.Message}");
            }
        }
    }
}