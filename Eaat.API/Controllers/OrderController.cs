using Eaat.Models;
using Eaat.RabbitMQService;
using Eaat.Resilience;
using Microsoft.AspNetCore.Mvc;

namespace Eaat.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly RabbitMQPublisher _publisher;

        public OrderController(RabbitMQPublisher publisher)
        {
            _publisher = publisher;
        }

        public async Task<IActionResult> PlaceOrder([FromBody] OrderPlaced order)
        {
            try
            {
                // Use API pipeline - shorter timeout, fewer retries as opposed to resilience handling for RabbitMQ
                await ResiliencePipelines.Api.ExecuteAsync(async ct =>
                {
                    await _publisher.PublishOrderPlacedAsync(order);
                });

                return Ok($"Order {order.OrderId} placed successfully");
            }
            catch (Exception ex)
            {
                // If all retries fail or circuit breaker is open, return 503
                return StatusCode(503, $"Service currently unavailable. Please try again later - {ex.Message}");
            }
        }
    }
}