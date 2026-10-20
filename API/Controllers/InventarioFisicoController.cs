using API.Services.Inventarios;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos.Inventarios;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventarioFisicoController(IInventarioFisicoService service) : ControllerBase
    {
        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetAllAsync()
        {
            var documentos = await service.GetAllAsync();
            return Ok(documentos);
        }

        [HttpGet]
        [Route("getbyid/{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var documento = await service.GetByIdAsync(id);
            if (documento == null) return NotFound($"Documento físico {id} no encontrado");
            return Ok(documento);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateAsync([FromBody] InventarioFisico documento)
        {
            if (documento == null) return BadRequest("Datos inválidos");
            var result = await service.CreateAsync(documento);
            return Ok(result);
        }

        [HttpPut]
        [Route("update/{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] InventarioFisico documento)
        {
            if (documento == null) return BadRequest("Datos inválidos");
            var result = await service.UpdateAsync(id, documento);
            return Ok(result);
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var deleted = await service.DeleteAsync(id);
            if (!deleted) return NotFound($"Documento físico {id} no encontrado");
            return NoContent();
        }

        [HttpGet]
        [Route("getnextnum")]
        public async Task<IActionResult> GetNextNumAsync()
        {
            var next = await service.GetNextNumAsync();
            return Ok(new { numero = next });
        }

        /// <summary>
        /// Compara el conteo físico contra un documento teórico. Es de solo lectura:
        /// no guarda diferencias ni modifica ningún documento.
        /// </summary>
        [HttpPost]
        [Route("comparar")]
        public async Task<IActionResult> CompararAsync([FromBody] ComparacionRequest request)
        {
            if (request == null) return BadRequest("Datos inválidos");

            try
            {
                var result = await service.CompararAsync(request.DocumentoTeorico, request.InventarioFisico);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Aplica el conteo físico al inventario de productos. Enviar documentoTeorico vacío
        /// para una aplicación "solo físico"; enviarlo para conciliar y poner en cero los
        /// productos del teórico que no se escanearon.
        /// </summary>
        [HttpPost]
        [Route("aplicar/{id}")]
        public async Task<IActionResult> AplicarAsync(int id, [FromBody] AplicarFisicoRequest? request)
        {
            var resultado = await service.AplicarAsync(id, request?.DocumentoTeorico);

            if (!resultado.Success)
                return BadRequest(resultado);

            return Ok(resultado);
        }
    }
}
