using API.Services.Suppliers;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuppliersController(ISuppliersService service, ILogger<SuppliersController> logger) : ControllerBase
    {
        private readonly ISuppliersService service = service;
        private readonly ILogger<SuppliersController> _logger = logger;

        [HttpGet]
        [Route("getsuppliers")]
        public async Task<IActionResult> GetSuppliersAsync()
        {
            var suppliers = await service.GetSuppliersAsync();
            return Ok(suppliers);
        }

        [HttpGet]
        [Route("getsupplierbyid/{id}")]
        public async Task<IActionResult> GetSupplierById(int id)
        {
            var supplier = await service.GetSupplierByIdAsync(id);
            if (supplier == null)
                return NotFound();
            return Ok(supplier);
        }

        [HttpPost]
        [Route("createsuppliers")]
        public async Task<IActionResult> CreateSuppliersAsync([FromBody] Supplier supplier)
        {
            var created = await service.CreateSupplierAsync(supplier);
            if (created == null)
                return Conflict("El proveedor no se pudo crear: el nombre está vacío o el RUC ya está registrado.");
            return CreatedAtAction(nameof(GetSupplierById), new { id = created.SupplierId }, created);
        }

        [HttpPut]
        [Route("updatesuppliers")]
        public async Task<IActionResult> UpdateSuppliersAsync([FromBody] ParametrosUpdateSuppliers parametros)
        {
            if (parametros?.supplier == null)
                return BadRequest("Invalid supplier data");

            var updated = await service.UpdateSupplierAsync(parametros.id, parametros.supplier);
            if (updated == null)
                return Conflict("El proveedor no existe, el nombre está vacío o el RUC ya está registrado.");
            return Ok(updated);
        }

        [HttpGet]
        [Route("getnextnum")]
        public async Task<IActionResult> GetNextNumAsync()
        {
            var next = await service.GetNextNumAsync();
            return Ok(new { numero = next });
        }

        [HttpDelete]
        [Route("deletesuppliers/{id}")]
        public async Task<IActionResult> DeleteSuppliersAsync(int id)
        {
            var deleted = await service.DeleteSupplierAsync(id);
            if (!deleted)
                return NotFound();
            return NoContent();
        }

        [HttpPost]
        [Route("import-excel")]
        [RequestSizeLimit(30_000_000)]
        public async Task<IActionResult> ImportSuppliersFromExcelAsync(IFormFile file)
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
                _logger.LogInformation("Importación de proveedores finalizada: insertados {Inserted}, actualizados {Updated}, omitidos {Skipped}",
                    result.Inserted, result.Updated, result.Skipped);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar proveedores desde Excel");
                return StatusCode(500, new SupplierImportResult
                {
                    Errors = [new SupplierImportError { Row = 0, Message = $"Error interno al procesar el archivo: {ex.Message}" }]
                });
            }
        }
    }
}