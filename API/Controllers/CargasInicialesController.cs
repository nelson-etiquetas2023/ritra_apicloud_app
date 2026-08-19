using API.Services.CargasIniciales;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos.CargasIniciales;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CargasInicialesController(ICargasInicialesService service) : ControllerBase
    {
        private readonly ICargasInicialesService _service = service;

        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetAllAsync()
        {
            var cargas = await _service.GetAllAsync();
            return Ok(cargas);
        }

        [HttpGet]
        [Route("getbyid/{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var carga = await _service.GetByIdAsync(id);
            if (carga == null) return NotFound($"Carga inicial {id} no encontrada");
            return Ok(carga);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateAsync([FromBody] Inicial inicial)
        {
            if (inicial == null) return BadRequest("Datos inválidos");
            var created = await _service.CreateAsync(inicial);
            return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
        }

        [HttpPut]
        [Route("update/{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] Inicial inicial)
        {
            var updated = await _service.UpdateAsync(id, inicial);
            if (updated == null) return NotFound($"Carga inicial {id} no encontrada");
            return Ok(updated);
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound($"Carga inicial {id} no encontrada");
            return NoContent();
        }

        [HttpPost]
        [Route("import")]
        public async Task<IActionResult> ImportAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se recibió ningún archivo.");

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest("El archivo debe ser un Excel (.xlsx o .xls).");

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var result = await _service.ImportFromExcelAsync(stream);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CargaInicialImportResult
                {
                    Success = false,
                    Errors = [new RowError { Row = 0, Message = $"Error interno al procesar el archivo: {ex.Message}" }]
                });
            }
        }

        [HttpGet]
        [Route("getDocumentsInitialsInventory")]
        public async Task<IActionResult> GetDocumentsInitialsInventoryAsync()
        {
            var documentos = await _service.GetDocumentsInitialsInventoryAsync();
            return Ok(documentos);
        }

        [HttpGet]
        [Route("template")]
        public IActionResult GetTemplate()
        {
            var bytes = _service.GenerateTemplate();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PlantillaCargaInicial.xlsx");
        }
    }
}