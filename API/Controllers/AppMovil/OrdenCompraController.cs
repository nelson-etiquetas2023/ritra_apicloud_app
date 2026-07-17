using API.Services.OcMovil;
using Shared.Dtos.Compras;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.AppMovil
{
    [ApiController]
    [Route("api/[controller]")]

    public class OrdenCompraController(IOcMovilService service) : ControllerBase
    {
        public IOcMovilService Service { get; set; } = service;

        [HttpGet]
        [Route("getorders")]
        public async Task<IEnumerable<OrdenCompra>> GetOrdersAsync()
        {
            var orders = await Service.GetOrdersAsync();
            return orders;
        }

        [HttpGet]
        [Route("getorderbyid/{Id}")]
        public async Task<IActionResult> GetOrderAsync(string Id) 
        {
            var order = await Service.GetOrderByIdAsync(Id);
            if (order == null) return NotFound($"Orden {Id} no encontrada");
            return Ok(order);
        }

        [HttpPost]
        [Route("addorder")]
        public async Task<IActionResult> AddOrder([FromBody] OrdenCompra oc) 
        {
            var OrderCreated = await Service.AddOrderAsync(oc);
            return OrderCreated is null ? 
            BadRequest("No se pudo crear la orden") : Ok(OrderCreated);
        }

        [HttpPut]
        [Route("updateorder/{Id}")]
        public async Task<IActionResult> UpdateOrderAsync(string Id, [FromBody] OrdenCompra Oc) 
        {
            var OrderUpdate = await Service.UpdateOrderAsync(Id, Oc);
            if(OrderUpdate == null) return NotFound($"Orden {Id} no encontrada");
            return Ok(OrderUpdate);
        }

        [HttpDelete]
        [Route("deleteorder/{Id}")]
        public async Task<IActionResult> DeleteOrderAsync(string Id) 
        {
            var OrderDeleted = await Service.DeleteOrderAsync(Id);
            if(!OrderDeleted) return NotFound($"Orden {Id} no encontrada");
            return Ok(OrderDeleted);    
        }



    }
}
