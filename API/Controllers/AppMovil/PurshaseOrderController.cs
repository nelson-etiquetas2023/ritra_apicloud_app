using API.Services.AppMovil;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos.AppMovil;

namespace API.Controllers.AppMovil
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurshaseOrderController(IAppMovilService Service) : ControllerBase
    {
        private IAppMovilService Service { get; set; } = Service;

        [HttpGet]
        [Route("getorderspurchase")]
        public async Task<IActionResult> GetOrdersPurchaseAsync() 
        {
            var orders = await Service.GetOrdersPurchaseAsync();
            return Ok(orders);
        }

        [HttpGet]
        [Route("getorderpurchasebyid/{orderid}")]
        public async Task<IActionResult> GetOrderPurchaseById(string orderid) 
        {
            var orders = await Service.GetOrderByIdAsync(orderid);

            if (orders == null) 
            {
                Console.WriteLine($"GetOrderById: Order {orderid} not found");
                return NotFound();
            }

            return Ok(orders);
        } 

        [HttpGet]
        [Route("createorder")]
        public async Task<IActionResult> CreateOrder(OrderPurchase order) 
        {
            var orderCreated = await Service.CreateOrderAsync(order); 
            return Ok(orderCreated);
        }

        [HttpPut]
        [Route("updateorderspurchase")]

        public async Task<IActionResult> UpdateOrderPurchase([FromBody] OrderPurchase order) 
        {
            var updated = await Service.UpdateOrderAsync(order);
            if (updated == null) NotFound();
            return Ok(updated);
        }

        [HttpDelete]
        [Route("deleteorderpurchase/{orderid}")]
        public async Task<IActionResult> DeleteOrderPurchase(string orderid) 
        {
            var deleted = await Service.DeleteOrderAsync(orderid);
            if (deleted == null) NotFound();
            return NoContent();
        }
    }
}
