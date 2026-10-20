using API.Services.Inventarios;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos.Inventarios;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventarioTeoricoController(IInventarioTeoricoService service) : ControllerBase
    {
        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetAllAsync()
        {
            var cargas = await service.GetAllAsync();
            return Ok(cargas);
        }

        [HttpGet]
        [Route("getbyid/{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var carga = await service.GetByIdAsync(id);
            if (carga == null) return NotFound($"Carga inicial {id} no encontrada");
            return Ok(carga);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateAsync([FromBody] InventarioTeorico inicial)
        {
            if (inicial == null) return BadRequest("Datos inválidos");
            var result = await service.CreateAsync(inicial);
            return Ok(result);
        }

        [HttpPut]
        [Route("update/{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] InventarioTeorico inicial)
        {
            if (inicial == null) return BadRequest("Datos inválidos");
            var result = await service.UpdateAsync(id, inicial);
            if (!result.Success && result.Data == null)
                return Ok(result);
            return Ok(result);
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var deleted = await service.DeleteAsync(id);
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

                var result = await service.ImportFromExcelAsync(stream);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new InventarioTeoricoImportResult
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
            var documentos = await service.GetDocumentsInitialsInventoryAsync();
            return Ok(documentos);
        }

        [HttpGet]
        [Route("getnextnum")]
        public async Task<IActionResult> GetNextNumAsync()
        {
            var next = await service.GetNextNumAsync();
            return Ok(new { numero = next });
        }

        [HttpGet]
        [Route("template")]
        public IActionResult GetTemplate()
        {
            var bytes = service.GenerateTemplate();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PlantillaCargaInicial.xlsx");
        }
    }
}