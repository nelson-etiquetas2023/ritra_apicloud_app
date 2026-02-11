using API.Services.Inventory;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   
    public class OrderFisicoController : ControllerBase
    {
        public IInventoryService service { get; set; }
        public OrderFisicoController(IInventoryService service)
        {
            this.service = service;
        }

        [HttpDelete]
        [Route("deletescanproducts/{id}")]
        public async Task<bool> DeleteScanProductsAsync(Guid id) 
        {
            var deleted = await service.DeleteScanProductsAsync(id);
            if(!deleted) return false;
            return true;
        }


        [HttpGet]
        [Route("getscanproducts/{OrderId}")]
        public async Task<List<ScanProducts>> GetScanProductsAsync(string OrderId) 
        {
            var productsScan = await service.GetScanProductsAsync(OrderId);
            return productsScan;
        }

        [HttpPost]
        [Route("savedatascanproducts")]
        public async Task<IActionResult> SaveDataProductScanAsync([FromBody] List<ScanProducts> productscan) 
        {
            var saved = await service.SaveDataProductScanAsync(productscan);
            return Ok(saved);
        }


        [HttpGet]
        [Route("getorders")]
        public async Task<IActionResult> GetOrdersAsync() 
        {
            var orders = await service.GetOrdersAsync();
            return Ok(orders);
        }

        [HttpGet]
        [Route("getorderbyid/{OrderNumber}")]
        public async Task<IActionResult> GetOrderByIdAsync(string OrderNumber) 
        {
            var order = await service.GetOrderByIdAsync(OrderNumber);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPost]
        [Route("createorder")]
        public async Task<IActionResult> CreateOrdersAsync([FromBody] OrderFisicoHeader order) 
        {
            var created = await service.CreateOrderAsync(order);
            return Ok(created);
        }

        [HttpPut]
        [Route("updateorder/{orderNumber}")]
        public async Task<ActionResult> UpdateOrdersAsync(string orderNumber, [FromBody] OrderFisicoHeader order) 
        {
            var updated = await service.UpdateOrderAsync(orderNumber, order);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete]
        [Route("deleteorder/{OrderNumber}")]
        public async Task<IActionResult> DeleteOrdersAsync(string OrderNumber) 
        {
            var deleted = await service.DeleteOrderAsync(OrderNumber);
            if(!deleted) return NotFound();
            return NoContent();
        }
    }
}
