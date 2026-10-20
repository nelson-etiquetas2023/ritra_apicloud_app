using API.Services.Picking;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos.Picking;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PickingController(IPickingService service) : ControllerBase
    {
        [HttpPost("transfer")]
        public async Task<IActionResult> TransferirAPicking([FromBody] TransferirAPickingRequest request)
        {
            var result = await service.TransferirAPickingAsync(request.NumerosPedido, request.Usuario);
            return Ok(result);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPedidosParaPicking()
        {
            var pedidos = await service.GetPedidosParaPickingAsync();
            return Ok(pedidos);
        }

        [HttpPost("complete/{id:int}")]
        public async Task<IActionResult> CompletarPicking(int id)
        {
            var ok = await service.CompletarPickingAsync(id);
            if (!ok) return NotFound($"El registro de picking {id} no existe o ya está completado.");
            return Ok(new { message = "Picking completado correctamente." });
        }
    }
}
