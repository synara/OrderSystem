using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Clients.Interfaces;
using OrderGenerator.DTO;

namespace OrderGenerator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private IQuickFixClient fixClient { get; set; }

        public OrderController(IQuickFixClient fixClient)
        {
            this.fixClient = fixClient;
        }

        [HttpPost]
        public IActionResult PostOrder(OrderDto order)
        {
            return Ok(fixClient.NewOrder(order));
        }

    }
}
