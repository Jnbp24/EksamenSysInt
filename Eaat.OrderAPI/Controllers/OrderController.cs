using Eaat.Models;
using Eaat.RabbitMQService;
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

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderPlaced order)
        {
            await _publisher.PublishOrderPlacedAsync(order);
            return Ok($"Order {order.OrderId} placed successfully");
        }
    }
}