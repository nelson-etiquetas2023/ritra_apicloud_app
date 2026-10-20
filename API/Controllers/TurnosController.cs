using API.Services.Turnos;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos.Turnos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TurnosController(ITurnosService service) : ControllerBase
    {
        [HttpGet]
        [Route("getabierto")]
        public async Task<IActionResult> GetAbiertoAsync([FromQuery] string caja = "Caja 1")
        {
            var turno = await service.GetAbiertoAsync(caja);
            if (turno == null) return NotFound($"No hay turno abierto en {caja}.");
            return Ok(turno);
        }

        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetAllAsync()
        {
            return Ok(await service.GetAllAsync());
        }

        [HttpGet]
        [Route("getbyid/{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var turno = await service.GetByIdAsync(id);
            if (turno == null) return NotFound($"Turno {id} no encontrado.");
            return Ok(turno);
        }

        [HttpGet]
        [Route("conteos/{id}")]
        public async Task<IActionResult> GetConteosAsync(int id)
        {
            return Ok(await service.GetConteosAsync(id));
        }

        [HttpGet]
        [Route("movimientos/{id}")]
        public async Task<IActionResult> GetMovimientosAsync(int id)
        {
            return Ok(await service.GetMovimientosAsync(id));
        }

        [HttpGet]
        [Route("resumen/{id}")]
        public async Task<IActionResult> GetResumenAsync(int id)
        {
            var resumen = await service.GetResumenAsync(id);
            if (resumen == null) return NotFound($"Turno {id} no encontrado.");
            return Ok(resumen);
        }

        [HttpPost]
        [Route("abrir")]
        public async Task<IActionResult> AbrirAsync([FromBody] AbrirTurnoRequest request)
        {
            if (request == null) return BadRequest("Datos inválidos.");
            var result = await service.AbrirAsync(request);
            return Ok(result);
        }

        [HttpPost]
        [Route("movimiento")]
        public async Task<IActionResult> AgregarMovimientoAsync([FromBody] TurnoMovimientoRequest request)
        {
            if (request == null) return BadRequest("Datos inválidos.");
            var result = await service.AgregarMovimientoAsync(request);
            return Ok(result);
        }

        [HttpPost]
        [Route("cerrar/{id}")]
        public async Task<IActionResult> CerrarAsync(int id, [FromBody] CerrarTurnoRequest request)
        {
            if (request == null) return BadRequest("Datos inválidos.");
            var result = await service.CerrarAsync(id, request);
            return Ok(result);
        }
    }
}
