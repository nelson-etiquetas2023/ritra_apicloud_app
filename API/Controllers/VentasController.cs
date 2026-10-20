using API.Services.Ventas;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos.Ventas;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController(IVentasService service) : ControllerBase
    {
        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetAllAsync()
        {
            var pedidos = await service.GetAllAsync();
            return Ok(pedidos);
        }

        [HttpGet]
        [Route("turno/{turnoId:int}")]
        public async Task<IActionResult> GetByTurnoAsync(int turnoId)
        {
            return Ok(await service.GetByTurnoAsync(turnoId));
        }

        [HttpGet]
        [Route("getbyid/{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var pedido = await service.GetByIdAsync(id);
            if (pedido == null) return NotFound($"Pedido de venta {id} no encontrado");
            return Ok(pedido);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateAsync([FromBody] PedidoVenta pedido)
        {
            if (pedido == null) return BadRequest("Datos inválidos");
            var result = await service.CreateAsync(pedido);
            return Ok(result);
        }

        [HttpPut]
        [Route("update/{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] PedidoVenta pedido)
        {
            if (pedido == null) return BadRequest("Datos inválidos");
            var result = await service.UpdateAsync(id, pedido);
            return Ok(result);
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var deleted = await service.DeleteAsync(id);
            if (!deleted) return NotFound($"Pedido de venta {id} no encontrado");
            return NoContent();
        }

        [HttpGet]
        [Route("getnextnum")]
        public async Task<IActionResult> GetNextNumAsync()
        {
            var next = await service.GetNextNumAsync();
            return Ok(new { numero = next });
        }

        [HttpPost]
        [Route("process/{numero}")]
        public async Task<IActionResult> ProcesarPedidoAsync(string numero)
        {
            var result = await service.ProcesarPedidoAsync(numero);
            return Ok(result);
        }

        [HttpPost]
        [Route("anular/{id}")]
        public async Task<IActionResult> Anular(int id)
        {
            var ok = await service.AnularAsync(id);
            if (!ok) return Conflict($"El pedido {id} no existe, ya está anulado o su turno está cerrado.");
            return Ok(new { message = "El pedido fue anulado." });
        }
    }
}
